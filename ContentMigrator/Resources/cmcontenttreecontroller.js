(function () {
	'use strict';

	angular
        .module('app')
        .controller('cmcontenttreecontroller', cmcontenttreecontroller);

	cmcontenttreecontroller.$inject = ['CMfactory', '$scope'];

	function cmcontenttreecontroller(CMfactory, $scope) {
		/* jshint validthis:true */
		var vm = this;
		vm.Open = false;
		vm.selected = "";
		vm.selectNode = function (id) {
			vm.selected = id;
		}
		$scope.init = function (nodeId, selectedId, events, server) {
			CMfactory.contentTree(nodeId, "master", server).then(function (response) {
				vm.data = response.data;
				if (server)
					events.server = server;
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
