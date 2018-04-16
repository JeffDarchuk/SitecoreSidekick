(function() {
    'use strict';

    angular
        .module('app')
        .directive('hgcontenttree', hgcontenttree);

	hgcontenttree.$inject = ['hgFactory', '$compile'];

    
	function hgcontenttree(hgFactory, $compile, attributes) {

        var directive = {
            templateUrl: "/scs/hg/resources/hgcontenttree.scs",
            restrict: 'E',
            scope: {
                parent: '=',
				events: '=',
				property: '=',
                selected: '='
            },
            compile: function(tElement, tAttr) {
                var contents = tElement.contents().remove();
                var compiledContents;
                return function(scope, iElement, iAttr) {
                    if (!compiledContents) {
                        compiledContents = $compile(contents);
                    }
                    compiledContents(scope, function(clone, scope) {
                        iElement.append(clone);
                    });
                };
            }
        }
        return directive;
    }

})();