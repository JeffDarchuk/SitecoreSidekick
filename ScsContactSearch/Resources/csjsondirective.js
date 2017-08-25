(function () {
	'use strict';

	angular
        .module('app')
		.directive('csjsondirective', csjsondirective);
	csjsondirective.$inject = ['$compile'];

	function csjsondirective($compile) {
		var directive = {
			templateUrl: "/scs/cs/resources/csjson.scs",
			restrict: 'E',
			scope: {
				parent: '=',
			},
			compile: function (tElement, tAttr) {
				var contents = tElement.contents().remove();
				var compiledContents;
				return function (scope, iElement, iAttr) {
					if (!compiledContents) {
						compiledContents = $compile(contents);
					}
					compiledContents(scope, function (clone, scope) {
						iElement.append(clone);
					});
				};
			}
		}
		return directive;
	}

})();