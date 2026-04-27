document.addEventListener('DOMContentLoaded', function () {
    const themeToggle = document.getElementById('theme-toggle');
    const htmlElement = document.documentElement;
    const iconElement = themeToggle.querySelector('i');

    // Verificar preferencia guardada o del SO
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const savedTheme = localStorage.getItem('theme') || (prefersDark ? 'dark' : 'light');

    // Aplicar tema inicial
    applyTheme(savedTheme);

    // Cambiar tema al hacer click
    themeToggle.addEventListener('click', function () {
        const currentTheme = localStorage.getItem('theme') || (prefersDark ? 'dark' : 'light');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        applyTheme(newTheme);
        localStorage.setItem('theme', newTheme);
    });

    function applyTheme(theme) {
        if (theme === 'dark') {
            htmlElement.classList.add('dark-mode');
            htmlElement.style.colorScheme = 'dark';
            // Cambiar icono a Sol (para indicar que se puede cambiar a claro)
            iconElement.className = 'bi bi-sun-fill';
        } else {
            htmlElement.classList.remove('dark-mode');
            htmlElement.style.colorScheme = 'light';
            // Cambiar icono a Luna (para indicar que se puede cambiar a oscuro)
            iconElement.className = 'bi bi-moon-fill';
        }
    }
});