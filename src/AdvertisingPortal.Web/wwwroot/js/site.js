(function () {
    "use strict";

    // Multi-image upload preview
    document.querySelectorAll(".js-image-input").forEach(function (input) {
        input.addEventListener("change", function () {
            var container = document.querySelector(".js-image-preview");
            if (!container) return;

            container.innerHTML = "";
            Array.prototype.forEach.call(input.files || [], function (file) {
                if (!file.type.startsWith("image/")) return;

                var img = document.createElement("img");
                img.src = URL.createObjectURL(file);
                img.alt = file.name;
                container.appendChild(img);
            });
        });
    });

    // Keep details page thumbnails in sync with the image carousel
    var gallery = document.getElementById("advertisementGallery");
    var thumbs = document.querySelectorAll(".js-gallery-thumb");

    if (gallery && thumbs.length) {
        gallery.addEventListener("slid.bs.carousel", function (event) {
            thumbs.forEach(function (thumb) { thumb.classList.remove("active"); });
            if (thumbs[event.to]) {
                thumbs[event.to].classList.add("active");
            }
        });
    }
})();
