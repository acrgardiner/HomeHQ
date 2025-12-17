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

