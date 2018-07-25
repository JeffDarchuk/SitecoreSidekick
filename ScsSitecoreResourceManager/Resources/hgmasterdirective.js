(function () {
	'use strict';

	angular
        .module('app')
        .directive('hgmasterdirective', hgmasterdirective);

	function hgmasterdirective() {

		var directive = {
			templateUrl: "/scs/hg/resources/hgmaster.scs",
			restrict: 'EA'
		};
		return directive;
	}

})();
