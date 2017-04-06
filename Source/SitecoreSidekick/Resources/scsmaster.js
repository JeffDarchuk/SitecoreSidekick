var scsActiveModule = "";
(function () {
	'use strict';

	angular
        .module('app')
        .controller('master', master);
	master.$inject = ['$cookies'];

	function master($cookies) {
		var vm = this;
		vm.selectSidekick = function (sk) {
			vm.sidekick = sk;
			setTimeout(function () {
				if ($(".scs-form").length === 0)
					vm.goHome();
				else {
					var active = $("#" + sk.replace(" ", "").toLowerCase());
					active.parent().parent().attr("style", active.attr("targetwidth"));
					$(".scs-form").css("max-height", $(window).height() * .8 + "px");
				}
			}, 1);
			$cookies.put("sidekick", sk);
			scsActiveModule = sk;
		}
		vm.goHome = function () {
			$(".scs-form").css("max-height", window.top.screen.height + "px");
			var active = $("#" + vm.sidekick.replace(" ", "").toLowerCase());
			active.parent().parent().attr("style", "width:600px;");
			vm.sidekick = "";
			$cookies.put("sidekick", "");
			scsActiveModule = "";
		}
		vm.selectSidekick($cookies.get("sidekick"));
	}
})();
$('.fancybox').fancybox({
	width: '100%', height: '100%', fitToView: true, autoSize: true
});
window.onload = function () {
	document.onkeydown = function (event) {
		if (window.parent && window.parent.document)
			window.parent.document.onkeydown(event);
	};
};
$(document).click(function (event) {
	if (event.target === $("#overlay")[0] || event.target === $("body")[0]) {
		window.top.document.getElementById("scs").style.display = "none";
		window.top.document.body.style.overflow = "";
	}
});