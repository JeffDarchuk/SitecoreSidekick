(function () {
	'use strict';

	angular
        .module('app')
        .directive('jmmasterdirective', jmmasterdirective);

	function jmmasterdirective() {

		var directive = {
			templateUrl: "/scs/jm/resources/jmmaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();
