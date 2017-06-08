(function () {
	'use strict';

	angular
        .module('app')
        .directive('ecmasterdirective', ecmasterdirective);

	function ecmasterdirective() {

		var directive = {
			templateUrl: "/scs/ec/resources/ecmaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();