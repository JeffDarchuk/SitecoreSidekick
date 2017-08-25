(function () {
	'use strict';

	angular
        .module('app')
		.controller('csjsoncontroller', csjsoncontroller);

	csjsoncontroller.$inject = ['csFactory', '$scope', '$cookies', '$interval'];

	function csjsoncontroller(csFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		$scope.init = function (parent) {
			vm.data = parent;
		}
		vm.rawValue = function(value) {
			return typeof (value) !== "object";
		}
		vm.hasProperties = function (o) {
			for (var prop in o)
				return true;
			return false;
		}
	}
})();
