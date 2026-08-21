// Dynamic "services offered" rows. Indexes are renumbered after every change so
// the ASP.NET model binder receives a contiguous Services[0..n] collection.
(function () {
    var container = document.getElementById('serviceRows');
    var addButton = document.getElementById('addServiceRow');
    if (!container || !addButton) return;

    function renumber() {
        var rows = container.querySelectorAll('.service-row');
        rows.forEach(function (row, index) {
            row.querySelectorAll('input').forEach(function (input) {
                var name = input.getAttribute('name');
                if (!name) return;
                input.setAttribute('name', name.replace(/Services\[\d+\]/, 'Services[' + index + ']'));
            });
        });
    }

    function buildRow() {
        var row = document.createElement('div');
        row.className = 'service-row row g-2 align-items-start mb-2';
        row.innerHTML =
            '<div class="col-12 col-md-4"><input name="Services[0].Name" class="form-control" placeholder="Service name" /></div>' +
            '<div class="col-12 col-md-4"><input name="Services[0].Details" class="form-control" placeholder="Short detail (optional)" /></div>' +
            '<div class="col-6 col-md-2"><input name="Services[0].PriceFrom" type="number" step="1" min="0" class="form-control" placeholder="From" /></div>' +
            '<div class="col-5 col-md-1"><input name="Services[0].PriceUnit" class="form-control" placeholder="Unit" /></div>' +
            '<div class="col-1 d-flex align-items-center"><button type="button" class="btn btn-sm btn-link text-danger remove-service-row" title="Remove"><i class="bi bi-x-lg"></i></button></div>';
        return row;
    }

    addButton.addEventListener('click', function () {
        container.appendChild(buildRow());
        renumber();
    });

    container.addEventListener('click', function (event) {
        var button = event.target.closest('.remove-service-row');
        if (!button) return;
        var rows = container.querySelectorAll('.service-row');
        if (rows.length === 1) {
            rows[0].querySelectorAll('input').forEach(function (input) { input.value = ''; });
            return;
        }
        button.closest('.service-row').remove();
        renumber();
    });
})();
