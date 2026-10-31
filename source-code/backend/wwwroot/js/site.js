document.addEventListener('DOMContentLoaded', function () {
    var modal = document.getElementById('confirmDelete');
    if (modal) {
        modal.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;
            modal.querySelector('form').setAttribute('action', btn.getAttribute('data-action'));
            modal.querySelector('.ten').textContent = btn.getAttribute('data-name');
        });
    }
    document.querySelectorAll('.auto-close').forEach(function (a) {
        setTimeout(function () { bootstrap.Alert.getOrCreateInstance(a).close(); }, 5000);
    });
});
