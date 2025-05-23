document.getElementById("httpForm").addEventListener("submit", async (e) => {
  e.preventDefault();

  const jwtToken = document.getElementById("jwtToken").value;
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

  const evaluateEndpoint = "/api/gk/evaluate";

  try {
    const evalHeaders = {
      "Content-Type": "application/json",
      "X-Forwarded-Uri": pathWithQuery,
      "X-Forwarded-Method": method,
    };
    if (jwtToken.trim()) {
      evalHeaders["Authorization"] = `Bearer ${jwtToken}`;
    }

    const evalResponse = await fetch(evaluateEndpoint, {
      method: "POST",
      headers: evalHeaders,
      body: JSON.stringify({ path: pathWithQuery, method }),
    });

    if (!evalResponse.ok) {
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
    alertBox.className = origResponse.ok
      ? "alert alert-success"
      : "alert alert-warning";
    alertBox.textContent = origResponse.ok
      ? `Success: ${origResponse.status}`
      : `Error: ${origResponse.status}`;
    alertBox.classList.remove("d-none");
  } catch (error) {
    alertBox.className = "alert alert-danger";
    alertBox.textContent = `Request error: ${error.message}`;
    alertBox.classList.remove("d-none");
  }
});

async function main() {
  if (localStorage.getItem("username") === null) {
    window.location.href = "../login/login.html";
  }
}

function logout() {
  localStorage.removeItem("username");
  window.location.href = "../login/login.html";
}

document.addEventListener("DOMContentLoaded", main());
