(function () {
	'use strict';

	angular
        .module('app')
        .controller('xcmastercontroller', xcmastercontroller);

	xcmastercontroller.$inject = ['xcFactory', '$scope', '$cookies', '$interval'];

	function xcmastercontroller(xcFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		vm.output = "";
		vm.models = new Array();
		vm.loading = true;
		xcFactory.getModels().then(function (response) {
			vm.loading = false;
			vm.models = response.data;
		});
		vm.downloadModel = function(name) {
			xcFactory.downloadModel(name).then(function(response) {
				alert(response.data);
			});
		}
	}
})();
