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

    // Details page gallery
    var main = document.getElementById("galleryMain");
    var thumbs = document.querySelectorAll(".js-gallery-thumb");

    if (main && thumbs.length) {
        thumbs[0].classList.add("active");
        thumbs.forEach(function (thumb) {
            thumb.addEventListener("click", function () {
                main.src = thumb.getAttribute("data-image-url");
                thumbs.forEach(function (t) { t.classList.remove("active"); });
                thumb.classList.add("active");
            });
        });
    }
})();
