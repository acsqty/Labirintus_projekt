function setLang(lang) {
    const elements = document.querySelectorAll("[data-hu]");

    elements.forEach(el => {
        const text = el.getAttribute("data-" + lang);

        if (text) {
            el.textContent = text;
        }
    });
}
function setLang(lang) {
    localStorage.setItem("lang", lang);

    document.querySelectorAll("[data-hu]").forEach(el => {
        el.innerHTML = lang === "hu" ? el.dataset.hu : el.dataset.en;
    });

    document.getElementById("langModal").style.display = "none";
}


window.onload = function () {
    let lang = localStorage.getItem("lang");

    if (!lang) {
        document.getElementById("langModal").style.display = "flex";
    } else {
        setLang(lang);
    }
};