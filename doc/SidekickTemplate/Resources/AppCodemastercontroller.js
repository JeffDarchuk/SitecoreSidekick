(function () {
	'use strict';

	angular
        .module('app')
        .controller('AppCodemastercontroller', AppCodemastercontroller);

	AppCodemastercontroller.$inject = ['AppCodeFactory', '$scope', '$cookies', '$interval'];

	function AppCodemastercontroller(AppCodeFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		vm.output = "";
		vm.addContent = function () {
			AppCodeFactory.contentDemo(vm.userInputContent).then(function(result) {
				vm.output += result.data;
			});
		}
	}
})();
