(function () {
	'use strict';

	angular
        .module('app')
        .controller('hgcontenttreecontroller', hgcontenttreecontroller);

	hgcontenttreecontroller.$inject = ['hgFactory', '$scope'];

	function hgcontenttreecontroller(hgFactory, $scope) {
		/* jshint validthis:true */
		var vm = this;
		vm.Open = false;
		vm.selected = "";
		vm.selectNode = function (id) {
			vm.selected = id;
		}
		$scope.init = function (nodeId, selectedId, property, events) {
			nodeId = nodeId.toUpperCase();
			if (!nodeId.startsWith("{"))
				nodeId = "{" + nodeId + "}";
			if (selectedId) {
				selectedId = selectedId.toUpperCase();
				if (!selectedId.startsWith("{"))
					selectedId = "{" + selectedId + "}";
			}
			hgFactory.contentTree(nodeId, "master").then(function (response) {
				vm.data = response.data;
				if (selectedId === nodeId) {
					events.click(vm.data, property);
				}
			}, function(response) {
				vm.error = response.data;
			});
			if (typeof (selectedId) !== "undefined" && typeof (property.relatedIds) !== "undefined")
				if (property.relatedIds[nodeId] || nodeId === "")
					vm.Open = true;
		}
	}
})();
