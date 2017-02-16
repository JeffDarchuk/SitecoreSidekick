(function () {
	'use strict';

	angular
        .module('app')
        .controller('cmcontenttreecontroller', cmcontenttreecontroller);

	cmcontenttreecontroller.$inject = ['CMfactory', 'ScsFactory', '$scope'];

	function cmcontenttreecontroller(CMfactory, ScsFactory, $scope) {
		/* jshint validthis:true */
		var vm = this;
		vm.Open = false;
		$scope.init = function (nodeId, selectedId, events, server, database) {
			CMfactory.contentTree(nodeId, database, server).then(function (response) {
				vm.data = response.data;
				if (server)
					events.server = server;
			}, function(response) {
				vm.error = response.data;
			});
			if (typeof (selectedId) !== "undefined" && selectedId.length > 0)
				ScsFactory.contentTreeSelectedRelated(nodeId, selectedId, server).then(function (response) {
					if (response.data)
						vm.Open = true;
				}, function (response) {
					vm.error = response.data;
				});

		}
	}
})();
