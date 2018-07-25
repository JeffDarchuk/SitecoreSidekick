var scsActiveModule = "";
(function () {
	'use strict';

	angular
        .module('app')
        .controller('master', master);
	master.$inject = ['$cookies', '$interval', 'ScsFactory'];

	function master($cookies, $interval, ScsFactory) {
		var vm = this;
		vm.valid = true;
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
		vm.validate = function() {
			ScsFactory.valid().then(function (result) {
				vm.valid = result.data === true;
				setTimeout(function()
				{
					vm.validate();
				}, 15000);
			});
		}
		vm.validate();
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
(function () {
	angular
		.module('app').directive('fallbackSrc', function () {
			var fallbackSrc = {
				link: function postLink(scope, iElement, iAttrs) {
					iElement.bind('error', function () {
						if (!iAttrs.altSrc || angular.element(this).attr("src") === iAttrs.altSrc) {
							angular.element(this).attr("src", iAttrs.fallbackSrc);
							return;
						}
						angular.element(this).attr("src", iAttrs.altSrc);
					});
				}
			}
			return fallbackSrc;
		});
})();
$('.fancybox').fancybox({
	width: '100%', height: '100%', fitToView: true, autoSize: true
});
$(document).click(function (event) {
	if (event.target === $("#overlay")[0] || event.target === $("body")[0]) {
		window.top.document.getElementById("scs").style.display = "none";
		window.top.document.body.style.overflow = "";
	}
});
