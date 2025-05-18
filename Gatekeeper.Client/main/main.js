// Add Rule Button
document.getElementById("addRuleBtn").addEventListener("click", () => {
  addRuleItem();
});

// Function to create a new rule item
function addRuleItem(
  rule = {
    Id: null,
    ContextSource: "JWT",
    Field: "",
    Operator: "Equal",
    Value: "",
    Endpoint: "",
    HttpType: "GET",
  }
) {
  const endpointList = document.getElementById("endpointList");
  const itemDiv = document.createElement("div");
  itemDiv.className = "endpoint-item";
  itemDiv.dataset.Id = rule.Id || Date.now(); // Unique ID for new items

  itemDiv.innerHTML = `
    <div class="row g-1 align-items-end">
        <div class="col-12 col-sm-2">
            <select class="form-select form-select-sm http-select">
                <option value="GET" ${
                  rule.HttpType === "GET" ? "selected" : ""
                }>GET</option>
                <option value="POST" ${
                  rule.HttpType === "POST" ? "selected" : ""
                }>POST</option>
                <option value="PUT" ${
                  rule.HttpType === "PUT" ? "selected" : ""
                }>PUT</option>
                <option value="PATCH" ${
                  rule.HttpType === "PATCH" ? "selected" : ""
                }>PATCH</option>
                <option value="DELETE" ${
                  rule.HttpType === "DELETE" ? "selected" : ""
                }>DELETE</option>
            </select>
        </div>
        <div class="col-12 col-sm-10">
            <input type="text" class="form-control form-control-sm endpoint-input" placeholder="Endpoint" value="${
              rule.Endpoint
            }">
        </div>
        <div class="col-12 col-sm-2">
            <select class="form-select form-select-sm source-select">
                <option value="JWT" ${
                  rule.ContextSource === "JWT" ? "selected" : ""
                }>JWT</option>
                <option value="Query" ${
                  rule.ContextSource === "Query" ? "selected" : ""
                } disabled>QUERY</option>
                <option value="Params" ${
                  rule.ContextSource === "Params" ? "selected" : ""
                } disabled>Params</option>
                <option value="Body" ${
                  rule.ContextSource === "Body" ? "selected" : ""
                } disabled>Body</option>
            </select>
        </div>
        <div class="col-12 col-sm-3">
            <input type="text" class="form-control form-control-sm field-input" placeholder="Field" value="${
              rule.Field
            }">
        </div>
        <div class="col-12 col-sm-3">
            <select class="form-select form-select-sm operation-select">
                <option value="Equal" ${
                  rule.Operator === "Equal" ? "selected" : ""
                }>Equals</option>
                <option value="NotEqual" ${
                  rule.Operator === "NotEqual" ? "selected" : ""
                }>Not Equals</option>
                <option value="GreaterThan" ${
                  rule.Operator === "GreaterThan" ? "selected" : ""
                }>Greater Than</option>
                <option value="LessThan" ${
                  rule.Operator === "LessThan" ? "selected" : ""
                }>Less Than</option>
            </select>
        </div>
        <div class="col-12 col-sm-3">
            <input type="text" class="form-control form-control-sm value-input" placeholder="Value" value="${
              rule.Value
            }">
        </div>
        <div class="col d-flex justify-content-end">
            <button class="btn btn-sm btn-light save-btn me-2">Save <i class="fa fa-save"></i></button>
            <button class="btn btn-sm btn-trash btn-danger"><i class="fa fa-trash"></i></button>
        </div>
    </div>
  `;

  // Prepend to list (newest at top)
  endpointList.prepend(itemDiv);

  // Save button event listener
  itemDiv.querySelector(".save-btn").addEventListener("click", async () => {
    const data = {
      Id: itemDiv.dataset.Id,
      ContextSource: itemDiv.querySelector(".source-select").value,
      Field: itemDiv.querySelector(".field-input").value,
      Operator: itemDiv.querySelector(".operation-select").value,
      Value: itemDiv.querySelector(".value-input").value,
      Endpoint: itemDiv.querySelector(".endpoint-input").value,
      HttpType: itemDiv.querySelector(".http-select").value,
    };

    try {
      let response;
      if (rule.Id) {
        response = await fetch(`/api/gk/rules/${itemDiv.dataset.Id}`, {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        });
      } else {
        response = await fetch(`/api/gk/rules`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(data),
        });
      }

      if (response.ok) {
        alert("Rule saved successfully.");
        if (!rule.Id) {
          itemDiv.dataset.Id = await response.json(); // Update ID for new items
        }
      } else {
        alert("Failed to save rule.");
      }
    } catch (error) {
      console.error("Save error:", error);
      alert("An error occurred while saving.");
    }
  });

  // Trash button event listener
  itemDiv.querySelector(".btn-trash").addEventListener("click", async () => {
    if (!confirm("Are you sure you want to delete this rule?")) return;

    try {
      const response = await fetch(`/api/gk/rules/${itemDiv.dataset.Id}`, {
        method: "DELETE",
      });

      if (response.ok) {
        itemDiv.remove();
        alert("Rule deleted successfully.");
      } else {
        alert("Failed to delete rule.");
      }
    } catch (error) {
      console.error("Delete error:", error);
      alert("An error occurred while deleting.");
    }
  });
}

// Load existing rules
async function loadRules() {
  try {
    const response = await fetch(`/api/gk/rules`);
    const rules = await response.json();
    console.log("Loaded rules:", rules);
    rules.forEach((rule) => addRuleItem(rule));
  } catch (error) {
    console.error("Load rules error:", error);
    alert("Failed to load rules.");
  }
}

async function main() {
  if (localStorage.getItem("username") === null) {
    window.location.href = "../login/login.html";
  } else {
    await loadRules();
  }
}

function logout() {
  localStorage.removeItem("username");
  window.location.href = "../login/login.html";
}

document.addEventListener("DOMContentLoaded", main());
