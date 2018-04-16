//from here: http://stackoverflow.com/questions/902407/how-to-hijack-key-combos-in-javascript

if (window.location.href.endsWith("Content%20Editor.aspx?sc_bw=1") || window.location.href.endsWith("default.aspx")) {
	window.onload = function() {
		document.onkeydown = function(event) {
			var keyCode;

			if (window.event) // IE/Safari/Chrome/Firefox(?)
			{
				keyCode = event.keyCode;
			} else if (event.which) // Netscape/Firefox/Opera
			{
				keyCode = event.which;
			}

			var keyChar = String.fromCharCode(keyCode).toLowerCase();

			if (keyChar === "s" && event.shiftKey && event.altKey) {
				var scs = window.top.document.getElementById("scs");

				if (typeof (scForm) !== "undefined" && typeof(scForm.postEvent) !== "undefined")
					return scForm.postEvent(this, event, 'scs:open');

				if (scs.style.display === "block") {
					scs.style.display = "none";
					window.top.document.body.style.height = "";
				} else {
					if (!scs.innerHtml)
						scs.innerHTML = "<iframe id='scs-iframe' frameBorder='0' style='width:100%;height:100%;background-color: transparent;' src='/scs/platform/scs.scs' />";
					scs.style.display = "block";
					scs.style.position = "absolute";
					window.top.scrollTo(0, 0);
					window.top.document.body.style.overflow = "hidden";
				}
				return false; // To prevent normal minimizing command
			} else if (keyCode === 27) {
				if (scs.style.display === "block") {
					scs.style.display = "none";
					window.top.document.body.style.overflow = "";
				}
			}
		};
	};

	document.innerHTML += "<div id='scs' style='display:none;height:100%;width:100%;position:absolute;z-index:9999;left:0;top:0;'></div>";
	document.write("<div id='scs' style='display:none;height:100%;width:100%;position:absolute;z-index:9999;left:0;top:0;'></div>");
}