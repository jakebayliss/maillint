// Loads the Office.js library.
Office.onReady();

// Helper function to add a status message to the notification bar.
function statusUpdate(icon, text, event) {
  const details = {
    type: Office.MailboxEnums.ItemNotificationMessageType.InformationalMessage,
    icon: icon,
    message: text,
    persistent: false
  };
  Office.context.mailbox.item.notificationMessages.replaceAsync("status", details, { asyncContext: event }, asyncResult => {
    const event = asyncResult.asyncContext;
    event.completed();
  });
}
// Displays a notification bar.
function defaultStatus(event) {
  statusUpdate("icon16" , "Hello World!", event);
}

// API base URL (update if your API port changes)
const API_BASE_URL = "https://localhost:7269";

async function callRewriteApi(text) {
  const response = await fetch(`${API_BASE_URL}/api/rewrite`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text })
  });
  if (!response.ok) {
    const err = await response.text().catch(() => "");
    throw new Error(`API error ${response.status}: ${err}`);
  }
  return response.json();
}

function rewriteInAI(event) {
  // Prefer rewriting only the selected text in compose
  Office.context.mailbox.item.getSelectedDataAsync(Office.CoercionType.Text, async sel => {
    if (sel.status !== Office.AsyncResultStatus.Succeeded) {
      statusUpdate("icon16", "Could not read selected text.", event);
      return;
    }

    const selected = (sel.value && sel.value.data) ? sel.value.data : "";
    if (!selected || !selected.trim()) {
      statusUpdate("icon16", "Select the text you want to rewrite, then click the button.", event);
      return;
    }

    try {
      statusUpdate("icon16", "Rewriting selection with AI…", event);
      const data = await callRewriteApi(selected);
      const rewritten = (data && data.text) ? data.text : "";
      Office.context.mailbox.item.setSelectedDataAsync(rewritten, { coercionType: Office.CoercionType.Text }, setResult => {
        if (setResult.status === Office.AsyncResultStatus.Succeeded) {
          statusUpdate("icon16", "Selection rewritten.", event);
        } else {
          statusUpdate("icon16", "Failed to update selection.", event);
        }
      });
    } catch (e) {
      statusUpdate("icon16", `Rewrite failed: ${e.message}`, event);
    }
  });
}

// Maps the function name specified in the manifest to its JavaScript counterpart.
Office.actions.associate("defaultStatus", defaultStatus);
Office.actions.associate("rewriteInAI", rewriteInAI);