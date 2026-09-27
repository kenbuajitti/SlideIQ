mergeInto(LibraryManager.library, {
  CodeOpenGames: function() {
    window.open('https://playiqgames.itch.io/', '_blank', 'noopener,noreferrer');
  },
  CodeDownload: function(namePtr, jsonPtr) {
    var blob = new Blob([UTF8ToString(jsonPtr)], {type: 'application/json'});
    var url = URL.createObjectURL(blob), link = document.createElement('a');
    link.href = url; link.download = UTF8ToString(namePtr); document.body.appendChild(link);
    link.click(); link.remove(); setTimeout(function() { URL.revokeObjectURL(url); }, 60000);
  },
  CodePickFile: function(receiverPtr) {
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
