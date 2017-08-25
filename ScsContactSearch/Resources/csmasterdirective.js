(function () {
	'use strict';

	angular
        .module('app')
        .directive('csmasterdirective', csmasterdirective);

	function csmasterdirective() {

		var directive = {
			templateUrl: "/scs/cs/resources/csmaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();