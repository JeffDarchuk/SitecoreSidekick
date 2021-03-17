(function () {
	'use strict';

	angular
        .module('app')
        .directive('xcmasterdirective', xcmasterdirective);

	function xcmasterdirective() {

		var directive = {
			templateUrl: "/scs/xc/resources/xcmaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();
