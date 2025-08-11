window.wheelzoomInterop = {
    initialize: function (imageId, options) {
        const img = document.getElementById(imageId);
        if (img) {
            // Default options
            const defaultOptions = {
                zoom: 0.10,
                maxZoom: 4,
                initialZoom: 1,
                initialX: 0.5,
                initialY: 0.5
            };

            // Merge with provided options
            const finalOptions = { ...defaultOptions, ...options };

            // Apply wheelzoom
            wheelzoom(img, finalOptions);

            // Add some styling for better UX
            img.style.cursor = 'grab';
            img.style.userSelect = 'none';

            // Change cursor when dragging
            img.addEventListener('mousedown', function() {
                img.style.cursor = 'grabbing';
            });

            img.addEventListener('mouseup', function() {
                img.style.cursor = 'grab';
            });

            return true;
        }
        return false;
    },

    destroy: function (imageId) {
        const img = document.getElementById(imageId);
        if (img && img.wheelzoom) {
            img.wheelzoom();
            img.style.cursor = '';
            img.style.userSelect = '';
            return true;
        }
        return false;
    },

    reset: function (imageId) {
        const img = document.getElementById(imageId);
        if (img) {
            // Destroy and reinitialize to reset zoom
            this.destroy(imageId);
            this.initialize(imageId);
            return true;
        }
        return false;
    }
};
