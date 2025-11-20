// Image Editor with Crop and Rotate functionality
class ImageEditor {
    constructor(containerId) {
        this.containerId = containerId;
        this.container = null;
        this.canvas = null;
        this.ctx = null;
        this.image = null;
        this.rotation = 0;
        this.scale = 1;
        this.offsetX = 0;
        this.offsetY = 0;
        this.isDragging = false;
        this.dragStartX = 0;
        this.dragStartY = 0;
        
        // Crop functionality
        this.cropEnabled = false;
        this.cropRect = null;
        this.isDraggingCrop = false;
        this.isResizingCrop = false;
        this.isCreatingCrop = false;
        this.resizeHandle = null;
        this.cropDragStartX = 0;
        this.cropDragStartY = 0;
        
        // Larger handle size for touch devices
        this.handleSize = this.isTouchDevice() ? 20 : 10;
        
        // Pinch zoom support
        this.lastTouchDistance = 0;
    }

    isTouchDevice() {
        return ('ontouchstart' in window) || (navigator.maxTouchPoints > 0);
    }

    async initialize(imageSrc) {
        this.container = document.getElementById(this.containerId);
        if (!this.container) {
            throw new Error(`Container with id '${this.containerId}' not found`);
        }

        // Clear existing content
        this.container.innerHTML = '';

        // Create canvas
        this.canvas = document.createElement('canvas');
        this.canvas.style.maxWidth = '100%';
        this.canvas.style.maxHeight = '100%';
        this.canvas.style.cursor = 'grab';
        this.ctx = this.canvas.getContext('2d');

        this.container.appendChild(this.canvas);

        // Load image
        await this.loadImage(imageSrc);

        // Setup event listeners
        this.setupEventListeners();

        // Initial draw
        this.draw();
    }

    loadImage(src) {
        return new Promise((resolve, reject) => {
            this.image = new Image();
            this.image.crossOrigin = 'anonymous';
            
            this.image.onload = () => {
                this.resetTransform();
                resolve();
            };
            
            this.image.onerror = () => {
                reject(new Error('Failed to load image'));
            };
            
            this.image.src = src;
        });
    }

    resetTransform() {
        this.rotation = 0;
        this.scale = 1;
        this.offsetX = 0;
        this.offsetY = 0;
        
        // Set canvas size based on container
        const containerRect = this.container.getBoundingClientRect();
        this.canvas.width = containerRect.width;
        this.canvas.height = containerRect.height;
        
        // Calculate initial scale to fit image in canvas
        this.fitToCanvas();
    }

    fitToCanvas() {
        if (!this.image) return;

        // Calculate the bounding box of the rotated image
        const rad = (this.rotation * Math.PI) / 180;
        const sin = Math.abs(Math.sin(rad));
        const cos = Math.abs(Math.cos(rad));
        
        // Dimensions of the rotated image bounding box
        const rotatedWidth = this.image.width * cos + this.image.height * sin;
        const rotatedHeight = this.image.width * sin + this.image.height * cos;
        
        const canvasWidth = this.canvas.width;
        const canvasHeight = this.canvas.height;

        // Calculate scale to fit rotated image in canvas
        const scaleX = canvasWidth / rotatedWidth;
        const scaleY = canvasHeight / rotatedHeight;
        this.scale = Math.min(scaleX, scaleY) * 0.85; // 0.85 to add padding and ensure full visibility

        // Reset position to center
        this.offsetX = 0;
        this.offsetY = 0;
    }

    setupEventListeners() {
        // Mouse events for panning
        this.canvas.addEventListener('mousedown', (e) => this.handleMouseDown(e));
        this.canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e));
        this.canvas.addEventListener('mouseup', () => this.handleMouseUp());
        this.canvas.addEventListener('mouseleave', () => this.handleMouseUp());

        // Touch events for mobile
        this.canvas.addEventListener('touchstart', (e) => this.handleTouchStart(e), { passive: false });
        this.canvas.addEventListener('touchmove', (e) => this.handleTouchMove(e), { passive: false });
        this.canvas.addEventListener('touchend', (e) => this.handleTouchEnd(e));
        this.canvas.addEventListener('touchcancel', (e) => this.handleTouchEnd(e));

        // Wheel event for zoom
        this.canvas.addEventListener('wheel', (e) => this.handleWheel(e));
    }

    handleMouseDown(e) {
        const rect = this.canvas.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        if (this.cropEnabled) {
            // If crop rectangle doesn't exist yet, start creating it
            if (!this.cropRect) {
                this.isCreatingCrop = true;
                this.cropDragStartX = mouseX;
                this.cropDragStartY = mouseY;
                this.cropRect = {
                    x: mouseX,
                    y: mouseY,
                    width: 0,
                    height: 0
                };
                this.canvas.style.cursor = 'crosshair';
                return;
            }

            // Check if clicking on resize handles
            const handle = this.getResizeHandle(mouseX, mouseY);
            if (handle) {
                this.isResizingCrop = true;
                this.resizeHandle = handle;
                this.cropDragStartX = mouseX;
                this.cropDragStartY = mouseY;
                return;
            }

            // Check if clicking inside crop area
            if (this.isInsideCropRect(mouseX, mouseY)) {
                this.isDraggingCrop = true;
                this.cropDragStartX = mouseX - this.cropRect.x;
                this.cropDragStartY = mouseY - this.cropRect.y;
                this.canvas.style.cursor = 'move';
                return;
            }

            // Clicking outside existing crop - start creating a new one
            this.isCreatingCrop = true;
            this.cropDragStartX = mouseX;
            this.cropDragStartY = mouseY;
            this.cropRect = {
                x: mouseX,
                y: mouseY,
                width: 0,
                height: 0
            };
            this.canvas.style.cursor = 'crosshair';
            return;
        }

        // Default image panning
        this.isDragging = true;
        this.dragStartX = e.clientX - this.offsetX;
        this.dragStartY = e.clientY - this.offsetY;
        this.canvas.style.cursor = 'grabbing';
    }

    handleMouseMove(e) {
        const rect = this.canvas.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        if (this.cropEnabled && !this.isDragging) {
            // Update cursor based on position
            if (this.cropRect && this.cropRect.width > 0 && this.cropRect.height > 0) {
                const handle = this.getResizeHandle(mouseX, mouseY);
                if (handle) {
                    this.canvas.style.cursor = this.getResizeCursor(handle);
                } else if (this.isInsideCropRect(mouseX, mouseY)) {
                    this.canvas.style.cursor = 'move';
                } else {
                    this.canvas.style.cursor = 'crosshair';
                }
            } else {
                this.canvas.style.cursor = 'crosshair';
            }
        }

        if (this.isCreatingCrop) {
            // Calculate the crop rectangle as user drags
            const startX = this.cropDragStartX;
            const startY = this.cropDragStartY;
            
            this.cropRect.x = Math.min(startX, mouseX);
            this.cropRect.y = Math.min(startY, mouseY);
            this.cropRect.width = Math.abs(mouseX - startX);
            this.cropRect.height = Math.abs(mouseY - startY);
            
            // Keep within canvas bounds
            this.cropRect.x = Math.max(0, this.cropRect.x);
            this.cropRect.y = Math.max(0, this.cropRect.y);
            this.cropRect.width = Math.min(this.cropRect.width, this.canvas.width - this.cropRect.x);
            this.cropRect.height = Math.min(this.cropRect.height, this.canvas.height - this.cropRect.y);
            
            this.draw();
            return;
        }

        if (this.isResizingCrop) {
            this.resizeCropRect(mouseX, mouseY);
            this.draw();
            return;
        }

        if (this.isDraggingCrop) {
            this.cropRect.x = mouseX - this.cropDragStartX;
            this.cropRect.y = mouseY - this.cropDragStartY;
            
            // Keep crop within canvas bounds
            this.cropRect.x = Math.max(0, Math.min(this.canvas.width - this.cropRect.width, this.cropRect.x));
            this.cropRect.y = Math.max(0, Math.min(this.canvas.height - this.cropRect.height, this.cropRect.y));
            
            this.draw();
            return;
        }

        if (this.isDragging) {
            this.offsetX = e.clientX - this.dragStartX;
            this.offsetY = e.clientY - this.dragStartY;
            this.draw();
        }
    }

    handleMouseUp() {
        if (this.isCreatingCrop) {
            this.isCreatingCrop = false;
            
            // If crop is too small, remove it
            const minSize = 10;
            if (this.cropRect.width < minSize || this.cropRect.height < minSize) {
                this.cropRect = null;
            }
            
            this.canvas.style.cursor = this.cropEnabled ? 'crosshair' : 'grab';
            this.draw();
        }
        
        this.isDragging = false;
        this.isDraggingCrop = false;
        this.isResizingCrop = false;
        this.resizeHandle = null;
        
        if (!this.isCreatingCrop) {
            this.canvas.style.cursor = this.cropEnabled ? 'crosshair' : 'grab';
        }
    }

    handleTouchStart(e) {
        e.preventDefault();
        
        if (e.touches.length === 2) {
            // Two-finger pinch zoom
            const touch1 = e.touches[0];
            const touch2 = e.touches[1];
            this.lastTouchDistance = this.getTouchDistance(touch1, touch2);
            return;
        }
        
        if (e.touches.length === 1) {
            const touch = e.touches[0];
            const rect = this.canvas.getBoundingClientRect();
            const touchX = touch.clientX - rect.left;
            const touchY = touch.clientY - rect.top;

            if (this.cropEnabled) {
                // If crop rectangle doesn't exist yet, start creating it
                if (!this.cropRect) {
                    this.isCreatingCrop = true;
                    this.cropDragStartX = touchX;
                    this.cropDragStartY = touchY;
                    this.cropRect = {
                        x: touchX,
                        y: touchY,
                        width: 0,
                        height: 0
                    };
                    return;
                }

                // Check if touching resize handles
                const handle = this.getResizeHandle(touchX, touchY);
                if (handle) {
                    this.isResizingCrop = true;
                    this.resizeHandle = handle;
                    this.cropDragStartX = touchX;
                    this.cropDragStartY = touchY;
                    return;
                }

                // Check if touching inside crop area
                if (this.isInsideCropRect(touchX, touchY)) {
                    this.isDraggingCrop = true;
                    this.cropDragStartX = touchX - this.cropRect.x;
                    this.cropDragStartY = touchY - this.cropRect.y;
                    return;
                }

                // Touching outside existing crop - start creating a new one
                this.isCreatingCrop = true;
                this.cropDragStartX = touchX;
                this.cropDragStartY = touchY;
                this.cropRect = {
                    x: touchX,
                    y: touchY,
                    width: 0,
                    height: 0
                };
                return;
            }

            // Default image panning
            this.isDragging = true;
            this.dragStartX = touch.clientX - this.offsetX;
            this.dragStartY = touch.clientY - this.offsetY;
        }
    }

    handleTouchMove(e) {
        e.preventDefault();
        
        if (e.touches.length === 2) {
            // Two-finger pinch zoom
            const touch1 = e.touches[0];
            const touch2 = e.touches[1];
            const currentDistance = this.getTouchDistance(touch1, touch2);
            
            if (this.lastTouchDistance > 0) {
                const delta = currentDistance / this.lastTouchDistance;
                this.scale *= delta;
                this.scale = Math.max(0.1, Math.min(10, this.scale));
                this.draw();
            }
            
            this.lastTouchDistance = currentDistance;
            return;
        }
        
        if (e.touches.length === 1) {
            const touch = e.touches[0];
            const rect = this.canvas.getBoundingClientRect();
            const touchX = touch.clientX - rect.left;
            const touchY = touch.clientY - rect.top;

            if (this.isCreatingCrop) {
                // Calculate the crop rectangle as user drags
                const startX = this.cropDragStartX;
                const startY = this.cropDragStartY;
                
                this.cropRect.x = Math.min(startX, touchX);
                this.cropRect.y = Math.min(startY, touchY);
                this.cropRect.width = Math.abs(touchX - startX);
                this.cropRect.height = Math.abs(touchY - startY);
                
                // Keep within canvas bounds
                this.cropRect.x = Math.max(0, this.cropRect.x);
                this.cropRect.y = Math.max(0, this.cropRect.y);
                this.cropRect.width = Math.min(this.cropRect.width, this.canvas.width - this.cropRect.x);
                this.cropRect.height = Math.min(this.cropRect.height, this.canvas.height - this.cropRect.y);
                
                this.draw();
                return;
            }

            if (this.isResizingCrop) {
                this.resizeCropRect(touchX, touchY);
                this.draw();
                return;
            }

            if (this.isDraggingCrop) {
                this.cropRect.x = touchX - this.cropDragStartX;
                this.cropRect.y = touchY - this.cropDragStartY;
                
                // Keep crop within canvas bounds
                this.cropRect.x = Math.max(0, Math.min(this.canvas.width - this.cropRect.width, this.cropRect.x));
                this.cropRect.y = Math.max(0, Math.min(this.canvas.height - this.cropRect.height, this.cropRect.y));
                
                this.draw();
                return;
            }

            if (this.isDragging) {
                this.offsetX = touch.clientX - this.dragStartX;
                this.offsetY = touch.clientY - this.dragStartY;
                this.draw();
            }
        }
    }

    getTouchDistance(touch1, touch2) {
        const dx = touch2.clientX - touch1.clientX;
        const dy = touch2.clientY - touch1.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }

    handleTouchEnd(e) {
        // Reset pinch zoom distance
        this.lastTouchDistance = 0;
        
        // Handle crop creation end
        if (this.isCreatingCrop) {
            this.isCreatingCrop = false;
            
            // If crop is too small, remove it
            const minSize = 10;
            if (this.cropRect.width < minSize || this.cropRect.height < minSize) {
                this.cropRect = null;
            }
            
            this.draw();
        }
        
        // Reset all dragging states
        this.isDragging = false;
        this.isDraggingCrop = false;
        this.isResizingCrop = false;
        this.resizeHandle = null;
    }

    handleWheel(e) {
        e.preventDefault();
        
        const delta = e.deltaY > 0 ? 0.9 : 1.1;
        this.scale *= delta;
        
        // Limit scale
        this.scale = Math.max(0.1, Math.min(10, this.scale));
        
        this.draw();
    }

    rotate(degrees) {
        this.rotation += degrees;
        this.rotation = this.rotation % 360;
        
        // Clear crop when rotating as it won't make sense anymore
        if (this.cropEnabled) {
            this.cropRect = null;
        }
        
        // Recalculate scale to fit rotated image in canvas
        this.fitToCanvas();
        
        this.draw();
    }

    draw() {
        if (!this.image || !this.ctx) return;

        // Clear canvas
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

        // Save context state
        this.ctx.save();

        // Move to center of canvas
        this.ctx.translate(this.canvas.width / 2, this.canvas.height / 2);

        // Apply offset
        this.ctx.translate(this.offsetX, this.offsetY);

        // Apply rotation
        this.ctx.rotate((this.rotation * Math.PI) / 180);

        // Apply scale and draw image centered
        const scaledWidth = this.image.width * this.scale;
        const scaledHeight = this.image.height * this.scale;
        
        this.ctx.drawImage(
            this.image,
            -scaledWidth / 2,
            -scaledHeight / 2,
            scaledWidth,
            scaledHeight
        );

        // Restore context state
        this.ctx.restore();

        // Draw crop overlay if enabled
        if (this.cropEnabled && this.cropRect && this.cropRect.width > 0 && this.cropRect.height > 0) {
            this.drawCropOverlay();
        }
    }

    drawCropOverlay() {
        // Draw darkened overlay on non-cropped areas
        this.ctx.save();
        
        // Darken everything
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Clear the crop area
        this.ctx.clearRect(this.cropRect.x, this.cropRect.y, this.cropRect.width, this.cropRect.height);
        
        // Redraw the image only in the crop area
        this.ctx.save();
        this.ctx.beginPath();
        this.ctx.rect(this.cropRect.x, this.cropRect.y, this.cropRect.width, this.cropRect.height);
        this.ctx.clip();
        
        this.ctx.translate(this.canvas.width / 2, this.canvas.height / 2);
        this.ctx.translate(this.offsetX, this.offsetY);
        this.ctx.rotate((this.rotation * Math.PI) / 180);
        
        const scaledWidth = this.image.width * this.scale;
        const scaledHeight = this.image.height * this.scale;
        
        this.ctx.drawImage(
            this.image,
            -scaledWidth / 2,
            -scaledHeight / 2,
            scaledWidth,
            scaledHeight
        );
        
        this.ctx.restore();
        
        // Draw crop rectangle border
        this.ctx.strokeStyle = '#fff';
        this.ctx.lineWidth = 2;
        this.ctx.strokeRect(this.cropRect.x, this.cropRect.y, this.cropRect.width, this.cropRect.height);
        
        // Draw grid lines (rule of thirds)
        this.ctx.strokeStyle = 'rgba(255, 255, 255, 0.5)';
        this.ctx.lineWidth = 1;
        
        // Vertical lines
        this.ctx.beginPath();
        this.ctx.moveTo(this.cropRect.x + this.cropRect.width / 3, this.cropRect.y);
        this.ctx.lineTo(this.cropRect.x + this.cropRect.width / 3, this.cropRect.y + this.cropRect.height);
        this.ctx.moveTo(this.cropRect.x + (this.cropRect.width * 2) / 3, this.cropRect.y);
        this.ctx.lineTo(this.cropRect.x + (this.cropRect.width * 2) / 3, this.cropRect.y + this.cropRect.height);
        this.ctx.stroke();
        
        // Horizontal lines
        this.ctx.beginPath();
        this.ctx.moveTo(this.cropRect.x, this.cropRect.y + this.cropRect.height / 3);
        this.ctx.lineTo(this.cropRect.x + this.cropRect.width, this.cropRect.y + this.cropRect.height / 3);
        this.ctx.moveTo(this.cropRect.x, this.cropRect.y + (this.cropRect.height * 2) / 3);
        this.ctx.lineTo(this.cropRect.x + this.cropRect.width, this.cropRect.y + (this.cropRect.height * 2) / 3);
        this.ctx.stroke();
        
        // Draw resize handles
        this.drawResizeHandles();
        
        this.ctx.restore();
    }

    drawResizeHandles() {
        const handles = this.getResizeHandlePositions();
        
        this.ctx.fillStyle = '#fff';
        this.ctx.strokeStyle = '#000';
        this.ctx.lineWidth = 1;
        
        for (const handle of handles) {
            this.ctx.fillRect(
                handle.x - this.handleSize / 2,
                handle.y - this.handleSize / 2,
                this.handleSize,
                this.handleSize
            );
            this.ctx.strokeRect(
                handle.x - this.handleSize / 2,
                handle.y - this.handleSize / 2,
                this.handleSize,
                this.handleSize
            );
        }
    }

    getResizeHandlePositions() {
        const { x, y, width, height } = this.cropRect;
        return [
            { name: 'nw', x, y },
            { name: 'n', x: x + width / 2, y },
            { name: 'ne', x: x + width, y },
            { name: 'e', x: x + width, y: y + height / 2 },
            { name: 'se', x: x + width, y: y + height },
            { name: 's', x: x + width / 2, y: y + height },
            { name: 'sw', x, y: y + height },
            { name: 'w', x, y: y + height / 2 }
        ];
    }

    getResizeHandle(mouseX, mouseY) {
        if (!this.cropRect) return null;
        
        const handles = this.getResizeHandlePositions();
        // Larger touch target for mobile devices
        const threshold = this.isTouchDevice() ? this.handleSize * 1.5 : this.handleSize;
        
        for (const handle of handles) {
            const dx = mouseX - handle.x;
            const dy = mouseY - handle.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance <= threshold) {
                return handle.name;
            }
        }
        
        return null;
    }

    isInsideCropRect(x, y) {
        if (!this.cropRect) return false;
        return x >= this.cropRect.x &&
               x <= this.cropRect.x + this.cropRect.width &&
               y >= this.cropRect.y &&
               y <= this.cropRect.y + this.cropRect.height;
    }

    getResizeCursor(handle) {
        const cursors = {
            'nw': 'nw-resize',
            'n': 'n-resize',
            'ne': 'ne-resize',
            'e': 'e-resize',
            'se': 'se-resize',
            's': 's-resize',
            'sw': 'sw-resize',
            'w': 'w-resize'
        };
        return cursors[handle] || 'default';
    }

    resizeCropRect(mouseX, mouseY) {
        const minSize = 50;
        const deltaX = mouseX - this.cropDragStartX;
        const deltaY = mouseY - this.cropDragStartY;
        
        const oldRect = { ...this.cropRect };
        
        switch (this.resizeHandle) {
            case 'nw':
                this.cropRect.x += deltaX;
                this.cropRect.y += deltaY;
                this.cropRect.width -= deltaX;
                this.cropRect.height -= deltaY;
                break;
            case 'n':
                this.cropRect.y += deltaY;
                this.cropRect.height -= deltaY;
                break;
            case 'ne':
                this.cropRect.y += deltaY;
                this.cropRect.width += deltaX;
                this.cropRect.height -= deltaY;
                break;
            case 'e':
                this.cropRect.width += deltaX;
                break;
            case 'se':
                this.cropRect.width += deltaX;
                this.cropRect.height += deltaY;
                break;
            case 's':
                this.cropRect.height += deltaY;
                break;
            case 'sw':
                this.cropRect.x += deltaX;
                this.cropRect.width -= deltaX;
                this.cropRect.height += deltaY;
                break;
            case 'w':
                this.cropRect.x += deltaX;
                this.cropRect.width -= deltaX;
                break;
        }
        
        // Enforce minimum size
        if (this.cropRect.width < minSize) {
            this.cropRect.x = oldRect.x;
            this.cropRect.width = oldRect.width;
        }
        if (this.cropRect.height < minSize) {
            this.cropRect.y = oldRect.y;
            this.cropRect.height = oldRect.height;
        }
        
        // Keep within canvas bounds
        if (this.cropRect.x < 0) {
            this.cropRect.width += this.cropRect.x;
            this.cropRect.x = 0;
        }
        if (this.cropRect.y < 0) {
            this.cropRect.height += this.cropRect.y;
            this.cropRect.y = 0;
        }
        if (this.cropRect.x + this.cropRect.width > this.canvas.width) {
            this.cropRect.width = this.canvas.width - this.cropRect.x;
        }
        if (this.cropRect.y + this.cropRect.height > this.canvas.height) {
            this.cropRect.height = this.canvas.height - this.cropRect.y;
        }
        
        this.cropDragStartX = mouseX;
        this.cropDragStartY = mouseY;
    }

    enableCrop() {
        this.cropEnabled = true;
        this.cropRect = null; // User will create crop by dragging
        this.canvas.style.cursor = 'crosshair';
        this.draw();
    }

    disableCrop() {
        this.cropEnabled = false;
        this.cropRect = null;
        this.canvas.style.cursor = 'grab';
        this.draw();
    }

    async getCroppedImageData() {
        const outputCanvas = document.createElement('canvas');
        const outputCtx = outputCanvas.getContext('2d', { alpha: false });

        if (this.cropEnabled && this.cropRect && this.cropRect.width > 0 && this.cropRect.height > 0) {
            // Calculate the resolution scale factor between display and original image
            // We want to work at original resolution, not canvas resolution
            const resolutionScale = 1 / this.scale;
            
            // Calculate output dimensions at original resolution
            const outputWidth = Math.round(this.cropRect.width * resolutionScale);
            const outputHeight = Math.round(this.cropRect.height * resolutionScale);
            
            outputCanvas.width = outputWidth;
            outputCanvas.height = outputHeight;

            // Create a high-resolution temporary canvas
            const tempCanvas = document.createElement('canvas');
            const tempCtx = tempCanvas.getContext('2d', { alpha: false });
            
            // Scale up the temp canvas to work at original image resolution
            const canvasScaleUp = resolutionScale;
            tempCanvas.width = Math.round(this.canvas.width * canvasScaleUp);
            tempCanvas.height = Math.round(this.canvas.height * canvasScaleUp);

            // Render the image at original resolution
            tempCtx.imageSmoothingEnabled = false;
            tempCtx.save();
            
            // Apply the same transformations but scaled up
            tempCtx.scale(canvasScaleUp, canvasScaleUp);
            tempCtx.translate(this.canvas.width / 2, this.canvas.height / 2);
            tempCtx.translate(this.offsetX, this.offsetY);
            tempCtx.rotate((this.rotation * Math.PI) / 180);
            
            const scaledWidth = this.image.width * this.scale;
            const scaledHeight = this.image.height * this.scale;
            
            tempCtx.drawImage(
                this.image,
                -scaledWidth / 2,
                -scaledHeight / 2,
                scaledWidth,
                scaledHeight
            );
            tempCtx.restore();

            // Extract the crop at high resolution
            const cropX = Math.round(this.cropRect.x * canvasScaleUp);
            const cropY = Math.round(this.cropRect.y * canvasScaleUp);
            const cropWidth = Math.round(this.cropRect.width * canvasScaleUp);
            const cropHeight = Math.round(this.cropRect.height * canvasScaleUp);
            
            outputCtx.imageSmoothingEnabled = false;
            outputCtx.drawImage(
                tempCanvas,
                cropX,
                cropY,
                cropWidth,
                cropHeight,
                0,
                0,
                outputWidth,
                outputHeight
            );
        } else {
            // Export the full rotated image at original resolution
            const rad = (this.rotation * Math.PI) / 180;
            const sin = Math.abs(Math.sin(rad));
            const cos = Math.abs(Math.cos(rad));
            
            const rotatedWidth = this.image.width * cos + this.image.height * sin;
            const rotatedHeight = this.image.width * sin + this.image.height * cos;

            outputCanvas.width = Math.round(rotatedWidth);
            outputCanvas.height = Math.round(rotatedHeight);

            // Draw rotated image at original resolution
            outputCtx.imageSmoothingEnabled = false;
            outputCtx.save();
            outputCtx.translate(outputCanvas.width / 2, outputCanvas.height / 2);
            outputCtx.rotate(rad);
            outputCtx.drawImage(
                this.image,
                -this.image.width / 2,
                -this.image.height / 2,
                this.image.width,
                this.image.height
            );
            outputCtx.restore();
        }

        // Convert to blob and then to byte array
        // Use JPEG at 0.92 quality for good quality with faster processing
        return new Promise((resolve) => {
            outputCanvas.toBlob(async (blob) => {
                const arrayBuffer = await blob.arrayBuffer();
                const byteArray = new Uint8Array(arrayBuffer);
                resolve(Array.from(byteArray));
            }, 'image/jpeg', 0.92);
        });
    }

    destroy() {
        if (this.canvas) {
            this.canvas.remove();
        }
        this.container = null;
        this.canvas = null;
        this.ctx = null;
        this.image = null;
    }
}

// Global instances storage
window.imageEditors = window.imageEditors || {};

// Export functions for .NET interop
window.ImageEditorInterop = {
    initialize: async function(containerId, imageSrc) {
        try {
            const editor = new ImageEditor(containerId);
            await editor.initialize(imageSrc);
            window.imageEditors[containerId] = editor;
            return true;
        } catch (error) {
            console.error('Failed to initialize image editor:', error);
            return false;
        }
    },

    rotate: function(containerId, degrees) {
        const editor = window.imageEditors[containerId];
        if (editor) {
            editor.rotate(degrees);
            return true;
        }
        return false;
    },

    enableCrop: function(containerId) {
        const editor = window.imageEditors[containerId];
        if (editor) {
            editor.enableCrop();
            return true;
        }
        return false;
    },

    disableCrop: function(containerId) {
        const editor = window.imageEditors[containerId];
        if (editor) {
            editor.disableCrop();
            return true;
        }
        return false;
    },

    getCroppedImageData: async function(containerId) {
        const editor = window.imageEditors[containerId];
        if (editor) {
            return await editor.getCroppedImageData();
        }
        return null;
    },

    destroy: function(containerId) {
        const editor = window.imageEditors[containerId];
        if (editor) {
            editor.destroy();
            delete window.imageEditors[containerId];
            return true;
        }
        return false;
    }
};

