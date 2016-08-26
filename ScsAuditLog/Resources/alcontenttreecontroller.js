(function () {
	'use strict';

	angular
        .module('app')
        .controller('alcontenttreecontroller', alcontenttreecontroller);

	alcontenttreecontroller.$inject = ['ALFactory', '$scope'];

	function alcontenttreecontroller(ALFactory, $scope) {
		/* jshint validthis:true */
		var vm = this;
		vm.Open = false;
		vm.selected = "";
		vm.selectNode = function (id) {
			vm.selected = id;
		}
		$scope.init = function (nodeId, selectedId, events) {
			ALFactory.contentTree(nodeId, "master").then(function (response) {
				vm.data = response.data;
				if (selectedId === nodeId) {
					events.selectedItem = vm.data.Id;
					events.click(vm.data);
				}
			}, function(response) {
				vm.error = response.data;
			});


		}
	}
})();
