(() => {
  const storageKey = "emailHubTheme";
  const switchButton = document.getElementById("themeSwitch");
  const root = document.documentElement;

  const getTheme = () => root.getAttribute("data-theme") === "light" ? "light" : "dark";

  const setTheme = (theme) => {
    root.setAttribute("data-theme", theme);
    localStorage.setItem(storageKey, theme);

    if (switchButton) {
      switchButton.setAttribute("aria-pressed", String(theme === "dark"));
      switchButton.setAttribute("aria-label", `Switch to ${theme === "dark" ? "light" : "dark"} mode`);
    }
  };

  setTheme(getTheme());

  switchButton?.addEventListener("click", () => {
    setTheme(getTheme() === "dark" ? "light" : "dark");
  });
})();
