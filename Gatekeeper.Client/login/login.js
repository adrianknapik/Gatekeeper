document.getElementById("loginForm").addEventListener("submit", async (e) => {
  e.preventDefault();
  const username = document.getElementById("username").value;
  const password = document.getElementById("password").value;

  try {
    const response = await fetch("http://127.0.0.1:5113/api/account/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });

    if (response.ok) {
      localStorage.setItem("username", username); // Store username in local storage
      window.location.href = "../main/main.html"; // Redirect to main page on successful login
    } else {
      alert("Login failed. Please check your credentials.");
    }
  } catch (error) {
    console.error("Login error:", error);
    alert("An error occurred during login.");
  }
});

async function main() {
  if (localStorage.getItem("username") !== null) {
    window.location.href = "../main/main.html";
  }
}

document.addEventListener("DOMContentLoaded", main());
