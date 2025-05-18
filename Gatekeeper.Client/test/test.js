document.getElementById("httpForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const userUrl = document.getElementById("httpUrl").value;
  const method = document.getElementById("httpMethod").value;
  const bodyText = document.getElementById("httpBody").value;

  const alertBox = document.getElementById("responseAlert");
  const responseText = document.getElementById("responseText");
  alertBox.classList.add("d-none");
  responseText.value = "";

  let pathWithQuery;
  try {
    const urlObj = new URL(userUrl, window.location.origin);
    pathWithQuery = urlObj.pathname + urlObj.search;
  } catch {
    const [pathOnly, query = ""] = userUrl.split("?");
    pathWithQuery = pathOnly + (query ? `?${query}` : "");
  }

  const evaluateEndpoint = `/api/gk/evaluate${pathWithQuery}`;

  try {
    const evalResponse = await fetch(evaluateEndpoint, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Forwarded-Uri": pathWithQuery,
        "X-Forwarded-Method": method,
      },
    });

    if (evalResponse.status === 403 || evalResponse.status === 500) {
      let evalResult;
      try {
        evalResult = await evalResponse.json();
      } catch {
        evalResult = { message: evalResponse.statusText };
      }
      responseText.value = JSON.stringify(evalResult, null, 2);
      alertBox.className = "alert alert-danger";
      alertBox.textContent = `Evaluate failed: ${evalResponse.status}`;
      alertBox.classList.remove("d-none");
      return;
    }

    const origOptions = {
      method,
      headers: { "Content-Type": "application/json" },
    };
    if (!["GET", "DELETE"].includes(method) && bodyText.trim()) {
      origOptions.body = bodyText;
    }

    const origResponse = await fetch(userUrl, origOptions);
    const contentType = origResponse.headers.get("content-type");
    let origResult;
    if (contentType && contentType.includes("application/json")) {
      origResult = await origResponse.json();
    } else {
      origResult = await origResponse.text();
    }

    responseText.value = JSON.stringify(origResult, null, 2);
    if (origResponse.ok) {
      alertBox.className = "alert alert-success";
      alertBox.textContent = `Success: ${origResponse.status}`;
    } else {
      alertBox.className = "alert alert-warning";
      alertBox.textContent = `Error: ${origResponse.status}`;
    }
    alertBox.classList.remove("d-none");
  } catch (error) {
    alertBox.className = "alert alert-danger";
    alertBox.textContent = `Request error: ${error.message}`;
    alertBox.classList.remove("d-none");
  }
});
