(function () {
    "use strict";

    // A stack keeps a confirmation opened from the assistant independent of its parent.
    const stack = [];
    const selector = "a[href],button,input,select,textarea,[tabindex]";

    function visible(element) {
        return element && element.isConnected && !element.closest("[hidden],[inert]") &&
            element.getClientRects().length > 0 && window.getComputedStyle(element).visibility !== "hidden";
    }

    function focusable(container) {
        return Array.from(container.querySelectorAll(selector)).filter(function (element) {
            return element.tabIndex >= 0 && !element.matches(":disabled") && visible(element);
        });
    }

    function top() { return stack[stack.length - 1]; }

    function focusFirst(entry) {
        const first = focusable(entry.container)[0];
        if (first) {
            first.focus();
        } else {
            if (!entry.container.hasAttribute("tabindex")) {
                entry.container.setAttribute("tabindex", "-1");
            }
            entry.container.focus();
        }
    }

    function activate(container, trigger) {
        if (!container || stack.some(function (entry) { return entry.container === container; })) return;
        stack.push({ container: container, trigger: trigger || document.activeElement });
        document.body.classList.add("overlay-open");
    }

    function deactivate(container) {
        const index = stack.findIndex(function (entry) { return entry.container === container; });
        if (index < 0) return;
        const entry = stack[index];
        // Closing a parent also releases any of its nested option sheets.
        const wasTop = index === stack.length - 1;
        stack.splice(index, 1);
        for (let i = stack.length - 1; i >= 0; i--) {
            if (container.contains(stack[i].container)) stack.splice(i, 1);
        }
        document.body.classList.toggle("overlay-open", stack.length > 0);
        const current = top();
        if (visible(entry.trigger) && !entry.trigger.matches(":disabled") &&
            (!current || current.container.contains(entry.trigger))) {
            entry.trigger.focus();
        } else if (current && (wasTop || !current.container.contains(document.activeElement))) {
            focusFirst(current);
        }
    }

    document.addEventListener("keydown", function (event) {
        const entry = top();
        if (!entry || event.key !== "Tab") return;
        const items = focusable(entry.container);
        const position = items.indexOf(document.activeElement);
        if (items.length === 0) {
            event.preventDefault();
            focusFirst(entry);
        } else if (position < 0 || (event.shiftKey && position === 0) || (!event.shiftKey && position === items.length - 1)) {
            event.preventDefault();
            items[event.shiftKey ? items.length - 1 : 0].focus();
        }
    }, true);

    document.addEventListener("focusin", function (event) {
        const entry = top();
        if (entry && !entry.container.contains(event.target)) focusFirst(entry);
    });

    window.StageUpModalFocus = {
        activate: activate,
        deactivate: deactivate,
        isTop: function (container) { return !!top() && top().container === container; }
    };
}());
