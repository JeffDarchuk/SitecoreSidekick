document.write("<div id='ecscs' class='scSearchResultsTable' style='display:none;position:absolute;z-index:100;left:50%;'><a title=\"/sitecore/content\" href=\"#\" " +
	"onclick=\"openCE('http://ddev/sitecore/shell/default.aspx?sc_lang=en#sitecore://master/{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}_qst_lang_eq_en&ver_eq_1')\"" +
	" class=\"active\"><img src=\"/temp/IconCache/People/16x16/cubes_blue.png\" border=\"0\" alt=\"\" width=\"16px\" height=\"16px\"><h2>Content</h2></a></div>");
if (window.location.href.indexOf("scs") === -1) {
	setTimeout(function () {
		if (typeof (instantSearch) !== "undefined") {
			document.getElementById("ecscs").style.display = "block";
			setInterval(function () {
				new Ajax.Request('/scs/ecgetrelated.json',
				{
					method: 'get',
					onSuccess: function (transport) {
						var data = transport.responseJSON || [];
						var container = $("ecscs");
						for (var i = 0; i < container.childNodes.length; i++) {
							container.childNodes[i].remove();
						}
						for (var i = 0; i < data.length; i++) {
							container.insert("<div>" + data[i].Id + "</div>");
						}
					}
				});
			},
				200);
		} else {
			var el = document.getElementById("ecscs");
			el.parentNode.removeChild(el);
		}
	},
		1000);
}
function openCE(url) {
	var hacky = false;
	if (instantSearch.results.length === 0) {
		instantSearch.results.push($$("div#ecscs")[0]);
		hacky = true;
	}
	instantSearch.launch(url);
	if (hacky) {
		instantSearch.results = [];
	}

}

