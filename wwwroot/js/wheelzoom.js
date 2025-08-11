/*!
	Wheelzoom 4.0.1
	license: MIT
	http://www.jacklmoore.com/wheelzoom
*/
window.wheelzoom = (function(){
	var defaults = {
		zoom: 0.10,
		maxZoom: false,
		initialZoom: 1,
		initialX: 0.5,
		initialY: 0.5,
	};

	var canvas = document.createElement('canvas');

	var main = function(img, options){
		if (!img || !img.nodeName || img.nodeName !== 'IMG') { return; }

		var settings = {};
		var width;
		var height;
		var bgWidth;
		var bgHeight;
		var bgPosX;
		var bgPosY;
		var previousEvent;
		var cachedDataUrl;

		function setSrcToBackground(img) {
			img.style.backgroundImage = 'url("'+img.src+'")';
			img.style.backgroundRepeat = 'no-repeat';
			canvas.width = img.naturalWidth;
			canvas.height = img.naturalHeight;
			cachedDataUrl = canvas.toDataURL();
			img.src = cachedDataUrl;
		}

		function updateBgStyle() {
			if (bgPosX > 0) {
				bgPosX = 0;
			} else if (bgPosX < width - bgWidth) {
				bgPosX = width - bgWidth;
			}

			if (bgPosY > 0) {
				bgPosY = 0;
			} else if (bgPosY < height - bgHeight) {
				bgPosY = height - bgHeight;
			}

			img.style.backgroundSize = bgWidth+'px '+bgHeight+'px';
			img.style.backgroundPosition = bgPosX+'px '+bgPosY+'px';
		}

		function reset() {
			bgWidth = width;
			bgHeight = height;
			bgPosX = bgPosY = 0;
			updateBgStyle();
		}

		function onwheel(e) {
			var deltaY = 0;

			e.preventDefault();

			if (e.deltaY) { // FireFox 17+ (IE9+, Chrome 31+)
				deltaY = e.deltaY;
			} else if (e.wheelDelta) {
				deltaY = -e.wheelDelta;
			}

			// As far as I know, there is no good cross-browser way to get the cursor position relative to the event target.
			// We have to calculate the target element's position relative to the document, and subtract that from the
			// cursor's position relative to the document.
			var rect = img.getBoundingClientRect();
			var offsetX = e.pageX - rect.left - window.pageXOffset;
			var offsetY = e.pageY - rect.top - window.pageYOffset;

			// Record the offset between the bg edge and cursor:
			var bgCursorX = offsetX - bgPosX;
			var bgCursorY = offsetY - bgPosY;

			// Use the previous offset to get the percent offset between the bg edge and cursor:
			var bgRatioX = bgCursorX/bgWidth;
			var bgRatioY = bgCursorY/bgHeight;

			// Update the bg size:
			if (deltaY < 0) {
				bgWidth += bgWidth*settings.zoom;
				bgHeight += bgHeight*settings.zoom;
			} else {
				bgWidth -= bgWidth*settings.zoom;
				bgHeight -= bgHeight*settings.zoom;
			}

			if (settings.maxZoom) {
				bgWidth = Math.min(width*settings.maxZoom, bgWidth);
				bgHeight = Math.min(height*settings.maxZoom, bgHeight);
			}

			// Take the percent offset and apply it to the new size:
			bgPosX = offsetX - (bgWidth * bgRatioX);
			bgPosY = offsetY - (bgHeight * bgRatioY);

			// Prevent zooming out beyond the starting size
			if (bgWidth <= width || bgHeight <= height) {
				reset();
			} else {
				updateBgStyle();
			}
		}

		function drag(e) {
			e.preventDefault();
			var pageX = e.pageX;
			var pageY = e.pageY;

			if (e.changedTouches) {
				pageX = e.changedTouches[0].pageX;
				pageY = e.changedTouches[0].pageY;
			}

			bgPosX += (pageX - previousEvent.pageX);
			bgPosY += (pageY - previousEvent.pageY);

			previousEvent = e;

			updateBgStyle();
		}

		function removeDrag() {
			document.removeEventListener('mouseup', removeDrag);
			document.removeEventListener('mousemove', drag);
		}

		// Make the background draggable
		function draggable(e) {
			e.preventDefault();
			previousEvent = e;
			document.addEventListener('mousemove', drag);
			document.addEventListener('mouseup', removeDrag);
		}

		function load() {
			if (img.src === cachedDataUrl) return;

			var computedStyle = window.getComputedStyle(img, null);
			width = parseInt(computedStyle.width, 10);
			height = parseInt(computedStyle.height, 10);
			bgWidth = width * settings.initialZoom;
			bgHeight = height * settings.initialZoom;
			bgPosX = -(bgWidth - width) * settings.initialX;
			bgPosY = -(bgHeight - height) * settings.initialY;

			setSrcToBackground(img);
			updateBgStyle();
		}

		var destroy = function (originalProperties) {
			img.onload = originalProperties.onload ? originalProperties.onload : null;
			img.onmousedown = originalProperties.onmousedown ? originalProperties.onmousedown : null;
			img.onmousemove = originalProperties.onmousemove ? originalProperties.onmousemove : null;
			img.onwheel = originalProperties.onwheel ? originalProperties.onwheel : null;
			img.src = originalProperties.src ? originalProperties.src : '';
			img.style.backgroundImage = originalProperties.backgroundImage ? originalProperties.backgroundImage : '';
			img.style.backgroundRepeat = originalProperties.backgroundRepeat ? originalProperties.backgroundRepeat : '';
			img.style.backgroundSize = originalProperties.backgroundSize ? originalProperties.backgroundSize : '';
			img.style.backgroundPosition = originalProperties.backgroundPosition ? originalProperties.backgroundPosition : '';
		};

		img.wheelzoom = destroy;

		// Save original values:
		var originalProperties = {
			onload: img.onload,
			onmousedown: img.onmousedown,
			onmousemove: img.onmousemove,
			onwheel: img.onwheel,
			src: img.src,
			backgroundImage: img.style.backgroundImage,
			backgroundRepeat: img.style.backgroundRepeat,
			backgroundSize: img.style.backgroundSize,
			backgroundPosition: img.style.backgroundPosition
		};

		// Add event listeners:
		img.onload = load;
		img.onmousedown = draggable;
		img.onwheel = onwheel;

		// Copy any provided options to our settings object:
		for (var key in defaults) { settings[key] = defaults[key]; }
		for (var key in options) { settings[key] = options[key]; }

		if (img.complete) {
			load();
		}
	};

	// Do nothing in IE8
	if (typeof window.getComputedStyle !== 'function') {
		return function(elements) {
			return elements;
		};
	} else {
		return function(elements, options) {
			if (elements && elements.length) {
				Array.prototype.forEach.call(elements, main, options);
			} else if (elements && elements.nodeName) {
				main(elements, options);
			}
			return elements;
		};
	}
}());
