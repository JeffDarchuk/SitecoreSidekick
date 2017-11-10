(function () {
	'use strict';

	angular
        .module('app')
        .directive('AppCodemasterdirective', AppCodemasterdirective);

	function AppCodemasterdirective() {

		var directive = {
			templateUrl: "/scs/AppCode/resources/AppCodemaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();
