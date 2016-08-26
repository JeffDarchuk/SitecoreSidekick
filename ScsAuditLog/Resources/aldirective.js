(function () {
	'use strict';

	angular
        .module('app')
        .directive('aldirective', aldirective);

	function aldirective() {

		var directive = {
			templateUrl: "/scs/almaster.scs",
			restrict: 'EA'
		};
		return directive;

		function link(scope, element, attrs) {
		}
	}

})();