(function() {
    'use strict';

    angular
        .module('app')
        .directive('cmcontenttree', cmcontenttree);

    cmcontenttree.$inject = ['CMfactory', '$compile'];

    
    function cmcontenttree(CMfactory, $compile, attributes) {

        var directive = {
            templateUrl: "/scs/cmcontenttree.scs",
            restrict: 'E',
            scope: {
                parent: '=',
                events: '=',
                selected: '=',
				server: '='
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