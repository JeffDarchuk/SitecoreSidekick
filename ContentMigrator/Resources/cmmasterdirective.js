(function () {
	'use strict';

	angular
        .module('app')
        .directive('cmmasterdirective', cmmasterdirective);

	function cmmasterdirective() {

		var directive = {
			templateUrl: "/scs/cm/resources/cmmaster.scs",
			restrict: 'EA'
		};
		return directive;

		function link(scope, element, attrs) {
		}
	}

})();