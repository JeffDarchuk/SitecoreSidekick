(function () {
	'use strict';

	angular
        .module('app')
        .controller('ecmastercontroller', ecmastercontroller);

	ecmastercontroller.$inject = ['ECFactory', '$scope', '$cookies', '$interval'];

	function ecmastercontroller(ECFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;

		ECFactory.getLocations().then(function (response) {
			vm.core = response.data.core;
			vm.master = response.data.master;
			vm.editor = response.data.editor;
		}, function (response) {
			vm.error = response.data;
		});

		vm.refreshHistory = function () {
			ECFactory.getItemHistory().then(function (response) {
				var items = response.data.items;
				if (items)
					items.reverse();
				vm.itemHistory = items;
			}, function (response) {
				vm.error = response.data;
			});
		}
		$interval(function () {
			vm.refreshHistory();
		}, 5000);
		vm.refreshHistory();
	}
})();
