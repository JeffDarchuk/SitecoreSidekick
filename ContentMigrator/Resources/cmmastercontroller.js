(function () {
	'use strict';

	angular
        .module('app')
        .controller('cmmastercontroller', cmmastercontroller);

	cmmastercontroller.$inject = ['CMfactory', '$scope', '$timeout'];

	function cmmastercontroller(CMfactory, $scope, $timeout) {
		/* jshint validthis:true */
		var vm = this;
		vm.children = true;
		vm.overwrite = true;
		vm.pullParent = true;
		vm.mirror = false;
		vm.server = "";
		vm.spinner = false;
		vm.events = {
			'click': function (val) {
				vm.events.selected = val;
			}
		};
		vm.pull = function () {
			vm.spinner = true;
			if (!vm.serverModified && vm.events.selected && vm.events.selected.Id)
				CMfactory.contentTreePullItem(vm.events.selected.Id, "master", vm.events.server, vm.children, vm.overwrite, vm.pullParent, vm.mirror).then(function (response) {
					vm.streamResults(response.data);
				}, function(response) {
					vm.error = response.data;
				});
			else {
				vm.spinner = false;
				vm.response = { "Error": "Unable to pull from selected remote sitecore node." };
			}
		}
		vm.streamResults = function(id) {
			vm.operationId = id;
			vm.spinner = true;
			vm.getStatus();
			vm.response.update.show = true;
			vm.response.lineNumber = 0;
		}
		vm.stopOperation = function() {
			CMfactory.stopOperation(vm.operationId);
		}
		vm.serverModified = true;
		vm.serverSubmit = function () {
			vm.serverModified = true;
			$timeout(function () { vm.serverModified = false }, 1);
		}
		CMfactory.contentTreeServerList().then(function (response) {
			vm.serverList = response.data;
		});
		vm.GetOperationsInProgress = function() {
			CMfactory.operations().then(function (response) {
				vm.completedOperations = new Array();
				vm.runningOperations = new Array();
				for (var i = 0; i < response.data.length; i++) {
					var tmp = response.data[i].RootNode;
					tmp["operationId"] = response.data[i].OperationId;
					if (response.data[i].Completed) {
						vm.completedOperations.push(tmp);
					} else {
						vm.runningOperations.push(tmp);
					}
				}
				vm.operationList = response.data;
				if (scsActiveModule === "Content Migrator")
					setTimeout(function() { vm.GetOperationsInProgress() }, 1000);
			});
		}
		vm.GetOperationsInProgress();
		vm.getStatus = function() {
			if (vm.operationId) {
				setTimeout(function () {
					if (vm.response.lineNumber > -1)
						CMfactory.operationStatus(vm.operationId, vm.response.lineNumber).then(function (response) {
							if (vm.response.lineNumber > -1) {
								for (var i = 0; i < response.data.length; i++) {
									vm.response.lineNumber++;
									if (typeof (response.data[i].Time) !== "undefined") {
										vm.spinner = false;
										vm.response.Items = response.data[i].Items;
										vm.response.Time = response.data[i].Time;
										return;
									} else {
										switch (response.data[i].Operation) {
										case "Update":
											vm.response.update.push(response.data[i]);
											break;
										case "Insert":
											vm.response.insert.push(response.data[i]);
											break;
										case "Recycle":
											vm.response.recycle.push(response.data[i]);
											break;
										default:
											vm.response.error.push(response.data[i]);
										}
									}
								}
							}
							vm.getStatus();
						}, vm.getStatus);
				}, 100);
			}
		}
		vm.reset = function() {
			vm.response = new Object();
			vm.response.update = new Array();
			vm.response.insert = new Array();
			vm.response.recycle = new Array();
			vm.response.error = new Array();
			vm.response.lineNumber = 0;
			vm.operationId = null;
		}
		vm.toggle = function(el) {
			vm.response.update.show = false;
			vm.response.insert.show = false;
			vm.response.recycle.show = false;
			vm.response.error.show = false;
			vm.response[el]['show'] = true;
		}
		vm.reset();
	}
})();
