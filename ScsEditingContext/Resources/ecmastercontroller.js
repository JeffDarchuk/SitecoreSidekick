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
		var running = $interval(function () {
			vm.refreshHistory();
			if (scsActiveModule !== "Editing Context")
				$interval.cancel(running);
		}, 1000);
		vm.refreshHistory();
		vm.refreshRelated = function () {
			ECFactory.getRelatedItems().then(function (response) {
				var items = response.data;
				vm.relatedItems = items;
			}, function (response) {
				vm.error = response.data;
			});
		}
		var related = $interval(function () {
			vm.refreshRelated();
			if (scsActiveModule !== "Editing Context")
				$interval.cancel(related);
		}, 1000);
		vm.refreshRelated();
		vm.refreshReferrers = function () {
			ECFactory.getReferrersItems().then(function (response) {
				var items = response.data;
				vm.referrersItems = items;
			}, function (response) {
				vm.error = response.data;
			});
		}
		var referrers = $interval(function () {
			vm.refreshReferrers();
			if (scsActiveModule !== "Editing Context")
				$interval.cancel(referrers);
		}, 1000);
		vm.refreshReferrers();
	}
})();
