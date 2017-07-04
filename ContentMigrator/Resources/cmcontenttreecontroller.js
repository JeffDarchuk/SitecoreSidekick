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
		vm.server = "";
		vm.buildDiff = function(status, id, events) {
			if (status !== "cmfieldchanged")
				return;
			CMfactory.getDiff(id, vm.server).then(function(response) {
				events.lastClicked = response.data;
				events.diff = id;
				vm.setupCompare(events.lastClicked.Compare, events.showAll, events);
			});
		}
		vm.setupCompare = function (compare, skipValidation, events) {
			for (var el in compare) {
				if (events.difflang === 'clean')
					events.difflang = 'none';
				compare[el].valid = false;
				for (var i = 0; i < compare[el].length; i++) {
					if (!skipValidation && !events.validateDiffRow(compare[el][i].Item1, compare[el][i].Item2)) {
						compare[el][i].valid = false;
					} else {
						if (events.difflang === 'none') {
							events.difflang = el;
						}
						compare[el].valid = true;
						compare[el][i].valid = true;
					}
				}
			}
		}
		$scope.init = function (nodeId, selectedId, events, server, database) {
			vm.events = events;
			vm.data = nodeId;
			vm.loading = true;
			if (typeof(nodeId) === "object")
				vm.data.loading = true;
			if (!server.MissingRemote) {
				vm.server = server;
				CMfactory.contentTree(nodeId.Id, database, server).then(function (response) {
					vm.loading = false;
					vm.data = response.data;
					if (server)
						events.server = server;
				}, function (response) {
					vm.error = response.data;
				});
			} else {
				vm.loading = false;
				vm.data = server;
				vm.data.loading = false;
				vm.data.Nodes = new Array();
			}
			if (typeof (selectedId) !== "undefined" && typeof (events.relatedIds) !== "undefined")
					if (events.relatedIds[nodeId.Id] || nodeId === "")
						vm.Open = true;
		}
	}
})();
