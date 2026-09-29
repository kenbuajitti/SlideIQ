mergeInto(LibraryManager.library, {
  SlideOpenGames: function() {
    var url = 'https://iqgamesonline.com/?iqreturn=1';
    // Open synchronously from the player's button click. Detach the opener
    // before loading the catalogue; the return control closes its own tab.
    var tab = window.open('about:blank', '_blank');
    if (tab) {
      tab.opener = null;
      tab.location.replace(url);
      return;
    }

    // A native link gives a blocked popup a second, direct user gesture.
    // Never navigate the Unity frame away from its running game.
    if (document.getElementById('iq-games-browse-dialog')) return;
    var previousFocus = document.activeElement;
    var shade = document.createElement('div');
    shade.id = 'iq-games-browse-dialog';
    shade.setAttribute('role', 'dialog');
    shade.setAttribute('aria-modal', 'true');
    shade.setAttribute('aria-label', 'Browse other IQ games');
    shade.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:rgba(0,0,0,.85);display:flex;align-items:center;justify-content:center;font:18px Arial,sans-serif;color:white;padding:24px;';
    var panel = document.createElement('div');
    panel.style.cssText = 'max-width:440px;background:#1b2034;padding:28px;border-radius:12px;line-height:1.5;';
    var message = document.createElement('p');
    message.textContent = 'Browse the other IQ games in a new tab. Your current game stays here.';
    var link = document.createElement('a');
    link.href = url;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';
    link.textContent = 'Open IQ Games website';
    link.style.cssText = 'display:block;color:#c8f582;margin:20px 0;';
    var cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.textContent = 'Stay in this game';
    cancel.style.cssText = 'padding:12px;font:inherit;cursor:pointer;';
    var dismiss = function () {
      shade.remove();
      if (previousFocus && previousFocus.focus) previousFocus.focus();
    };
    cancel.onclick = dismiss;
    link.onclick = function () { window.setTimeout(dismiss, 100); };
    shade.onkeydown = function (event) {
      if (event.key === 'Escape') dismiss();
      if (event.key === 'Tab') {
        event.preventDefault();
        (document.activeElement === link ? cancel : link).focus();
      }
    };
    panel.appendChild(message);
    panel.appendChild(link);
    panel.appendChild(cancel);
    shade.appendChild(panel);
    // A fullscreen canvas cannot render DOM children; leave fullscreen
    // before displaying the browser-native fallback.
    if (document.fullscreenElement && document.exitFullscreen) {
      var exit = document.exitFullscreen();
      if (exit && exit.catch) exit.catch(function () {});
    }
    document.body.appendChild(shade);
    link.focus();
  },
  SlideDownload: function(namePtr, jsonPtr) {
    var blob = new Blob([UTF8ToString(jsonPtr)], {type: 'application/json'});
    var url = URL.createObjectURL(blob), link = document.createElement('a');
    link.href = url; link.download = UTF8ToString(namePtr); document.body.appendChild(link);
    link.click(); link.remove(); setTimeout(function() { URL.revokeObjectURL(url); }, 60000);
  },
  SlidePickFile: function(receiverPtr) {
    var receiver = UTF8ToString(receiverPtr), input = document.createElement('input');
    input.type = 'file'; input.accept = '.json,application/json'; input.style.display = 'none';
    document.body.appendChild(input);
    input.addEventListener('cancel', function() { input.remove(); });
    input.onchange = function() {
      var file = input.files[0]; input.remove(); if (!file) return;
      if (file.size > 2000000) { SendMessage(receiver, 'OnFileError', 'Maximum file size is 2 MB.'); return; }
      var reader = new FileReader();
      reader.onload = function() { SendMessage(receiver, 'OnImport', String(reader.result)); };
      reader.onerror = function() { SendMessage(receiver, 'OnFileError', 'The selected file could not be read.'); };
      reader.readAsText(file);
    };
    input.click();
  }
});
