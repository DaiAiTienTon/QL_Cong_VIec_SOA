

            let airports = [];

            // Load dữ liệu từ file JSON
            async function loadAirports() {
        try {
            const res = await fetch("/data/airports.json");
            if (res.ok) {
                airports = await res.json();
            } else {
                console.error("Không tải được airports.json");
            }
        } catch (err) {
                console.error("Lỗi fetch airports.json:", err);
        }
    }

            function setupAutocomplete(inputId, listId) {
        const inputEl = document.getElementById(inputId);
            const listEl = document.getElementById(listId);

        inputEl.addEventListener("input", () => {
            const keyword = inputEl.value.trim().toLowerCase();
            listEl.innerHTML = "";

            if (keyword.length < 2) {
                listEl.style.display = "none";
            return;
            }

            const results = airports.filter(a =>
            (a.AirportName && a.AirportName.toLowerCase().includes(keyword)) ||
            (a.City && a.City.toLowerCase().includes(keyword)) ||
            (a.IataCode && a.IataCode.toLowerCase().includes(keyword))
            );

            if (results.length === 0) {
                listEl.style.display = "none";
            return;
            }

            results.forEach(a => {
                const li = document.createElement("li");
            li.classList.add("suggestion-item");
            li.innerHTML = `
            <span class="suggestion-icon">✈️</span>
            <div class="suggestion-text">
                <div class="suggestion-main">${a.AirportName} (${a.IataCode})</div>
                <div class="suggestion-sub">${a.City}, ${a.Country}</div>
            </div>
            `;
                li.addEventListener("mousedown", () => {
                    // Hiển thị đẹp cho người dùng
                    inputEl.value = `${a.AirportName} (${a.IataCode})`;

                    // Gán IATA code vào input hidden
                    if (inputEl.id === "to") {
                        document.getElementById("toIata").value = a.IataCode;
                    }
                    if (inputEl.id === "from") {
                        document.getElementById("fromIata").value = a.IataCode;
                    }

                    listEl.style.display = "none";
                });



            listEl.appendChild(li);
            });

            listEl.style.display = "block";
        });

        // Ẩn khi click ra ngoài
        document.addEventListener("click", (e) => {
            if (!listEl.contains(e.target) && e.target !== inputEl) {
                listEl.style.display = "none";
            }
        });
    }

    // Gọi fetch trước rồi setup autocomplete
    loadAirports().then(() => {
                setupAutocomplete("from", "from-suggestions");
            setupAutocomplete("to", "to-suggestions");
    });




