// Add User Button
document.getElementById("addUserBtn").addEventListener("click", () => {
  addUserItem();
});

// Function to create a new user item
function addUserItem(user = { id: null, username: "", password: "" }) {
  const userList = document.getElementById("userList");
  const itemDiv = document.createElement("div");
  itemDiv.className = "user-item";
  itemDiv.dataset.id = user.id || Date.now(); // Unique ID for new items

  itemDiv.innerHTML = `
      <div class="row g-1 align-items-end">
        <div class="col-12 col-sm-4">
          <input type="text" class="form-control form-control-sm username-input" placeholder="Username" value="${
            user.username
          }">
        </div>
        <div class="col-12 col-sm-4">
          <input type="password" class="form-control form-control-sm password-input" placeholder="Password" value="${
            user.password
          }">
        </div>
        <div class="col d-flex justify-content-end">
          ${
            !user.id
              ? '<button class="btn btn-sm btn-light save-btn me-2">Save <i class="fa fa-save"></i></button>'
              : ""
          }
          <button class="btn btn-sm btn-trash btn-danger" ${
            !user.password ? "disabled" : ""
          }>
            Delete <i class="fa fa-trash"></i>
          </button>
        </div>
      </div>
    `;

  // Prepend to list (newest at top)
  userList.prepend(itemDiv);

  // Enable/disable delete button based on password input
  const passwordInput = itemDiv.querySelector(".password-input");
  const deleteBtn = itemDiv.querySelector(".btn-trash");
  passwordInput.addEventListener("input", () => {
    deleteBtn.disabled = !passwordInput.value;
  });

  // Save button event listener (only for new users)
  const saveBtn = itemDiv.querySelector(".save-btn");
  if (saveBtn) {
    saveBtn.addEventListener("click", async () => {
      const data = {
        id: itemDiv.dataset.id,
        username: itemDiv.querySelector(".username-input").value,
        password: itemDiv.querySelector(".password-input").value,
      };

      try {
        const response = await fetch(`/api/gk/account/register`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        });

        if (response.ok) {
          saveBtn.remove(); // Remove save button after saving
          deleteBtn.disabled = !data.password; // Update delete button state
        } else {
          alert("Failed to save user.");
        }
      } catch (error) {
        console.error("Save error:", error);
        alert("An error occurred while saving.");
      }
    });
  }

  // Trash button event listener
  deleteBtn.addEventListener("click", async () => {
    const userList = document.getElementById("userList");
    if (userList.children.length === 1) {
      alert("You cannot delete the last user.");
      return;
    }

    if (!confirm("Are you sure you want to delete this user?")) return;

    try {
      let username = itemDiv.querySelector(".username-input").value;
      let password = itemDiv.querySelector(".password-input").value;

      const response = await fetch(`/api/gk/account/delete`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      if (response.ok) {
        itemDiv.remove();
      } else {
        alert("Failed to delete user.");
      }
    } catch (error) {
      console.error("Delete error:", error);
      alert("An error occurred while deleting.");
    }
  });
}

// Load existing users
async function loadUsers() {
  try {
    const response = await fetch(`/api/gk/account/usernames`);
    const usernames = await response.json();

    usernames.usernames.forEach(
      (username) => addUserItem({ id: 1, username, password: "" }) // Use a truthy id to prevent save button
    );
  } catch (error) {
    console.error("Load users error:", error);
    alert("Failed to load users.");
  }
}

async function mainUsers() {
  if (localStorage.getItem("username") === null) {
    window.location.href = "../login/login.html";
  } else {
    await loadUsers();
  }
}

function logout() {
  localStorage.removeItem("username");
  window.location.href = "../login/login.html";
}

// Load users on page load
document.addEventListener("DOMContentLoaded", mainUsers());
