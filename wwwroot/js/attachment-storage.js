// IndexedDB-based storage for pending attachments
// Keeps image data in browser to survive SignalR disconnections

class AttachmentStorage {
    constructor() {
        this.dbName = 'HomeHQ_Attachments';
        this.storeName = 'pending_attachments';
        this.db = null;
    }

    async init() {
        if (this.db) return this.db;

        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, 1);

            request.onerror = () => reject(request.error);

            request.onsuccess = () => {
                this.db = request.result;
                resolve(this.db);
            };

            request.onupgradeneeded = (event) => {
                const db = event.target.result;
                if (!db.objectStoreNames.contains(this.storeName)) {
                    const store = db.createObjectStore(this.storeName, { keyPath: 'id' });
                    store.createIndex('sessionId', 'sessionId', { unique: false });
                }
            };
        });
    }

    // Store an attachment with its image data
    async store(id, sessionId, metadata, imageData) {
        await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);

            const record = {
                id: id,
                sessionId: sessionId,
                metadata: metadata,
                imageData: imageData, // Uint8Array or base64 string
                timestamp: Date.now()
            };

            const request = store.put(record);
            request.onsuccess = () => resolve(id);
            request.onerror = () => reject(request.error);
        });
    }

    // Get a single attachment by ID
    async get(id) {
        await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const request = store.get(id);

            request.onsuccess = () => resolve(request.result || null);
            request.onerror = () => reject(request.error);
        });
    }

    // Get all attachments for a session
    async getBySession(sessionId) {
        await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readonly');
            const store = transaction.objectStore(this.storeName);
            const index = store.index('sessionId');
            const request = index.getAll(sessionId);

            request.onsuccess = () => resolve(request.result || []);
            request.onerror = () => reject(request.error);
        });
    }

    // Update image data (after crop/rotate)
    async updateImageData(id, imageData) {
        await this.init();

        const existing = await this.get(id);
        if (!existing) return false;

        existing.imageData = imageData;
        existing.timestamp = Date.now();

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const request = store.put(existing);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    // Remove an attachment
    async remove(id) {
        await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const request = store.delete(id);

            request.onsuccess = () => resolve(true);
            request.onerror = () => reject(request.error);
        });
    }

    // Clear all attachments for a session (after successful save)
    async clearSession(sessionId) {
        await this.init();

        const items = await this.getBySession(sessionId);
        
        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);

            let deleted = 0;
            for (const item of items) {
                const request = store.delete(item.id);
                request.onsuccess = () => {
                    deleted++;
                    if (deleted === items.length) resolve(deleted);
                };
                request.onerror = () => reject(request.error);
            }

            if (items.length === 0) resolve(0);
        });
    }

    // Clean up old entries (older than 24 hours)
    async cleanup() {
        await this.init();

        const cutoff = Date.now() - (24 * 60 * 60 * 1000);

        return new Promise((resolve, reject) => {
            const transaction = this.db.transaction([this.storeName], 'readwrite');
            const store = transaction.objectStore(this.storeName);
            const request = store.openCursor();

            let deleted = 0;
            request.onsuccess = (event) => {
                const cursor = event.target.result;
                if (cursor) {
                    if (cursor.value.timestamp < cutoff) {
                        cursor.delete();
                        deleted++;
                    }
                    cursor.continue();
                } else {
                    resolve(deleted);
                }
            };
            request.onerror = () => reject(request.error);
        });
    }

    // Get image data as base64 data URL for display
    getDataUrl(record) {
        if (!record || !record.imageData) return null;
        
        const contentType = record.metadata?.contentType || 'image/jpeg';
        
        if (typeof record.imageData === 'string') {
            // Already base64
            return `data:${contentType};base64,${record.imageData}`;
        } else if (record.imageData instanceof Uint8Array || Array.isArray(record.imageData)) {
            // Convert to base64
            const bytes = record.imageData instanceof Uint8Array ? record.imageData : new Uint8Array(record.imageData);
            let binary = '';
            for (let i = 0; i < bytes.length; i++) {
                binary += String.fromCharCode(bytes[i]);
            }
            const base64 = btoa(binary);
            return `data:${contentType};base64,${base64}`;
        }
        
        return null;
    }
}

// Global instance
window.attachmentStorage = new AttachmentStorage();

// Blazor reconnection handler
window.BlazorReconnect = {
    _dotNetRef: null,
    _isConnected: true,
    _monitorInterval: null,
    _reconnectAttempts: 0,

    // Register a .NET component to receive reconnection notifications
    register: function(dotNetRef) {
        this._dotNetRef = dotNetRef;
        this._isConnected = true;
        this._reconnectAttempts = 0;
        
        // Start monitoring for disconnection/reconnection
        this._startConnectionMonitor();
        
        // Also observe the reconnect modal for changes
        this._observeReconnectModal();
    },

    unregister: function() {
        this._dotNetRef = null;
        if (this._monitorInterval) {
            clearInterval(this._monitorInterval);
            this._monitorInterval = null;
        }
        if (this._observer) {
            this._observer.disconnect();
            this._observer = null;
        }
    },

    _observeReconnectModal: function() {
        // Use MutationObserver to detect when reconnect modal appears/disappears
        const targetNode = document.body;
        const config = { childList: true, subtree: true, attributes: true, attributeFilter: ['class', 'style'] };

        this._observer = new MutationObserver((mutations) => {
            this._checkConnectionState();
        });

        this._observer.observe(targetNode, config);
    },

    _startConnectionMonitor: function() {
        // Check connection status periodically as backup
        if (this._monitorInterval) {
            clearInterval(this._monitorInterval);
        }
        
        this._monitorInterval = setInterval(() => {
            this._checkConnectionState();
        }, 500); // Check every 500ms for faster detection
    },

    _checkConnectionState: function() {
        const wasConnected = this._isConnected;
        
        // Check multiple indicators of disconnection
        const reconnectModal = document.getElementById('components-reconnect-modal');
        const reconnectShow = reconnectModal && 
            (reconnectModal.classList.contains('components-reconnect-show') ||
             getComputedStyle(reconnectModal).display !== 'none');
        
        // Also check for the "Attempting to reconnect" text
        const reconnectingText = document.querySelector('.components-reconnect-show');
        
        const isDisconnected = reconnectShow || !!reconnectingText;
        this._isConnected = !isDisconnected;

        // Detect state changes
        if (wasConnected && !this._isConnected) {
            console.log('Blazor connection lost');
            this._reconnectAttempts = 0;
        } else if (!wasConnected && this._isConnected) {
            console.log('Blazor connection restored');
            this._onReconnected();
        }
    },

    _onReconnected: async function() {
        this._reconnectAttempts++;
        console.log('Blazor reconnected (attempt ' + this._reconnectAttempts + ') - notifying components');
        
        // Small delay to ensure Blazor is fully ready
        await new Promise(resolve => setTimeout(resolve, 100));
        
        if (this._dotNetRef) {
            try {
                await this._dotNetRef.invokeMethodAsync('OnCircuitReconnected');
            } catch (e) {
                console.warn('Failed to notify .NET of reconnection:', e);
                // Retry after a short delay
                setTimeout(() => this._retryNotification(), 500);
            }
        }
    },

    _retryNotification: async function() {
        if (this._dotNetRef && this._isConnected) {
            try {
                await this._dotNetRef.invokeMethodAsync('OnCircuitReconnected');
                console.log('Retry notification succeeded');
            } catch (e) {
                console.warn('Retry notification failed:', e);
            }
        }
    },

    // Check if we're currently connected
    isConnected: function() {
        return this._isConnected;
    },

    // Manual trigger for testing
    triggerReconnected: async function() {
        await this._onReconnected();
    }
};

// Camera capture that bypasses Blazor's file handling entirely
window.CameraCapture = {
    // Hidden file input element
    _input: null,
    _resolveCapture: null,
    _sessionId: null,

    // Initialize hidden input element
    init: function() {
        if (this._input) return;

        this._input = document.createElement('input');
        this._input.type = 'file';
        this._input.accept = 'image/*';
        this._input.style.display = 'none';
        this._input.id = 'camera-capture-input';
        document.body.appendChild(this._input);

        this._input.addEventListener('change', async (e) => {
            if (this._resolveCapture && e.target.files && e.target.files.length > 0) {
                try {
                    const results = [];
                    for (const file of e.target.files) {
                        const result = await this._processFile(file);
                        results.push(result);
                    }
                    this._resolveCapture(results);
                } catch (error) {
                    console.error('Camera capture error:', error);
                    this._resolveCapture(null);
                }
            } else {
                // User cancelled
                this._resolveCapture(null);
            }
            // Reset input for next use
            this._input.value = '';
        });
    },

    // Process a single file - read and store in IndexedDB
    _processFile: async function(file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();

            reader.onload = async () => {
                try {
                    const arrayBuffer = reader.result;
                    const bytes = new Uint8Array(arrayBuffer);
                    const id = crypto.randomUUID();

                    const metadata = {
                        fileName: file.name,
                        contentType: file.type || 'image/jpeg',
                        size: file.size,
                        extension: file.name.includes('.') ? file.name.substring(file.name.lastIndexOf('.')) : '.jpg'
                    };

                    // Store in IndexedDB
                    await window.attachmentStorage.store(id, this._sessionId, metadata, Array.from(bytes));

                    // Get data URL for display
                    const record = await window.attachmentStorage.get(id);
                    const dataUrl = window.attachmentStorage.getDataUrl(record);

                    resolve({
                        id: id,
                        metadata: metadata,
                        dataUrl: dataUrl
                    });
                } catch (error) {
                    reject(error);
                }
            };

            reader.onerror = () => reject(reader.error);
            reader.readAsArrayBuffer(file);
        });
    },

    // Open camera (with capture attribute)
    openCamera: function(sessionId) {
        this.init();
        this._sessionId = sessionId;
        this._input.capture = 'environment'; // Use back camera
        this._input.multiple = false;

        return new Promise((resolve) => {
            this._resolveCapture = resolve;
            this._input.click();
        });
    },

    // Open gallery (no capture attribute)
    openGallery: function(sessionId, multiple = false) {
        this.init();
        this._sessionId = sessionId;
        this._input.removeAttribute('capture');
        this._input.multiple = multiple;

        return new Promise((resolve) => {
            this._resolveCapture = resolve;
            this._input.click();
        });
    },

    // Open file picker for any file type
    openFilePicker: function(sessionId, accept = 'image/*,application/pdf', multiple = true) {
        this.init();
        this._sessionId = sessionId;
        this._input.accept = accept;
        this._input.removeAttribute('capture');
        this._input.multiple = multiple;

        return new Promise((resolve) => {
            this._resolveCapture = resolve;
            this._input.click();
        });
    }
};

// Interop functions for .NET
window.AttachmentStorageInterop = {
    // Initialize and cleanup old entries
    init: async function() {
        await window.attachmentStorage.init();
        await window.attachmentStorage.cleanup();
        return true;
    },

    // Store file from input element directly (avoids SignalR transfer)
    storeFromFile: async function(id, sessionId, file) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            
            reader.onload = async () => {
                try {
                    const arrayBuffer = reader.result;
                    const bytes = new Uint8Array(arrayBuffer);
                    
                    const metadata = {
                        fileName: file.name,
                        contentType: file.type,
                        size: file.size,
                        extension: file.name.substring(file.name.lastIndexOf('.'))
                    };

                    await window.attachmentStorage.store(id, sessionId, metadata, Array.from(bytes));
                    
                    // Return the data URL for immediate display
                    const record = await window.attachmentStorage.get(id);
                    const dataUrl = window.attachmentStorage.getDataUrl(record);
                    
                    resolve({
                        id: id,
                        metadata: metadata,
                        dataUrl: dataUrl
                    });
                } catch (error) {
                    reject(error);
                }
            };
            
            reader.onerror = () => reject(reader.error);
            reader.readAsArrayBuffer(file);
        });
    },

    // Store from file input by element ID
    storeFromFileInput: async function(inputId, id, sessionId) {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) {
            return null;
        }
        
        const results = [];
        for (const file of input.files) {
            const fileId = id || crypto.randomUUID();
            const result = await window.AttachmentStorageInterop.storeFromFile(fileId, sessionId, file);
            results.push(result);
        }
        
        return results;
    },

    // Get data URL for display
    getDataUrl: async function(id) {
        const record = await window.attachmentStorage.get(id);
        return window.attachmentStorage.getDataUrl(record);
    },

    // Get all pending for a session (for recovery)
    getSessionAttachments: async function(sessionId) {
        const records = await window.attachmentStorage.getBySession(sessionId);
        return records.map(r => ({
            id: r.id,
            metadata: r.metadata,
            dataUrl: window.attachmentStorage.getDataUrl(r)
        }));
    },

    // Update after crop/rotate
    updateImageData: async function(id, imageDataArray) {
        const bytes = new Uint8Array(imageDataArray);
        await window.attachmentStorage.updateImageData(id, Array.from(bytes));
        
        // Return new data URL
        const record = await window.attachmentStorage.get(id);
        return window.attachmentStorage.getDataUrl(record);
    },

    // Get raw bytes for upload to server
    getImageBytes: async function(id) {
        const record = await window.attachmentStorage.get(id);
        if (!record || !record.imageData) return null;
        
        if (Array.isArray(record.imageData)) {
            return record.imageData;
        } else if (record.imageData instanceof Uint8Array) {
            return Array.from(record.imageData);
        } else if (typeof record.imageData === 'string') {
            // Decode base64
            const binary = atob(record.imageData);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++) {
                bytes[i] = binary.charCodeAt(i);
            }
            return Array.from(bytes);
        }
        
        return null;
    },

    // Remove single attachment
    remove: async function(id) {
        return await window.attachmentStorage.remove(id);
    },

    // Clear all for session (after save)
    clearSession: async function(sessionId) {
        return await window.attachmentStorage.clearSession(sessionId);
    },

    // Check if we have pending attachments (for recovery prompt)
    hasPendingAttachments: async function(sessionId) {
        const records = await window.attachmentStorage.getBySession(sessionId);
        return records.length > 0;
    }
};

