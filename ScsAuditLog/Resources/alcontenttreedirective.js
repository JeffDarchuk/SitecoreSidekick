(function() {
    'use strict';

    angular
        .module('app')
        .directive('alcontenttree', alcontenttree);

    alcontenttree.$inject = ['ALFactory', '$compile'];

    
    function alcontenttree(ALFactory, $compile, attributes) {

        var directive = {
            templateUrl: "/scs/alcontenttree.scs",
            restrict: 'E',
            scope: {
                parent: '=',
                events: '=',
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