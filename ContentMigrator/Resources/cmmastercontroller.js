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
		vm.eventDisabler = false;
		vm.bulkUpdate = false;
		vm.server = "";
		vm.spinner = false;
		vm.isPreview = false;
		vm.events = {
			'click': function (val) {
				vm.events.selected = val;
			}
		};
		vm.pull = function (preview) {
			if (preview || confirm("Are you sure you would like to pull content from the item " + vm.events.selected.DisplayName)) {
				vm.spinner = true;
				if (!vm.serverModified && vm.events.selected && vm.events.selected.Id)
					CMfactory.contentTreePullItem(vm.events.selected.Id, vm.events.selected.DatabaseName, vm.events.server, vm.children, vm.overwrite, vm.pullParent, vm.mirror, preview, vm.eventDisabler, vm.bulkUpdate).then(function (response) {
						vm.streamResults(response.data, vm.events.server, vm.events.selected.Id, vm.events.selected.DisplayName, preview);
					}, function(response) {
						vm.error = response.data;
					});
				else {
					vm.spinner = false;
					vm.response = { "Error": "Unable to pull from selected remote sitecore node." };
				}
			}
		}
		vm.streamResults = function(id, server, itemId, name, preview) {
			vm.operationId = id;
			vm.spinner = true;
			vm.getStatus();
			vm.streaming = new Object();
			vm.streaming.server = server;
			vm.streaming.id = itemId;
			vm.streaming.name = name;
			vm.response = new Object();
			vm.response.lineNumber = 0;
			vm.response.viewingNone = true;
			vm.isPreview = preview;
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
				vm.previewOperations = new Array();
				vm.cancelledOperations = new Array();
				for (var i = 0; i < response.data.length; i++) {
					var tmp = response.data[i].RootNode;
					tmp["operationId"] = response.data[i].OperationId;
					if (response.data[i].IsPreview) {
						vm.previewOperations.push(tmp);
					}else if (response.data[i].Cancelled) {
						vm.cancelledOperations.push(tmp);
					}else if (response.data[i].Completed) {
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
		vm.runPreview = function () {
			if (confirm("Are you sure you would like to execute this preview operation?")) {
				CMfactory.runPreviewAsPull(vm.operationId);
				vm.streamResults(vm.operationId, vm.streaming.server, vm.streaming.id, vm.streaming.name, false);
			}
		}
		vm.GetOperationsInProgress();
		vm.getStatus = function() {
			if (vm.operationId) {
				setTimeout(function () {
					if (vm.response.lineNumber > -1)
						CMfactory.queuedItems(vm.operationId).then(function(response) {
							vm.queuedItems = response.data;
						});
						CMfactory.operationStatus(vm.operationId, vm.response.lineNumber).then(function (response) {
							if (vm.response.lineNumber > -1) {
								var ending = null;
								for (var i = 0; i < response.data.length; i++) {
									vm.response.lineNumber++;
									if (typeof (response.data[i].Time) !== "undefined") {
										ending = response.data[i];
									} else {
										if (typeof (vm.response[response.data[i].Operation]) === "undefined") {
											vm.response[response.data[i].Operation] = new Array();
											vm.response[response.data[i].Operation].name = response.data[i].Operation;
											if (vm.response.viewingNone) {
												vm.response.viewingNone = false;
												vm.response[response.data[i].Operation].show = true;
											}
										}
										vm.response[response.data[i].Operation].push(response.data[i]);
									}
								}
								if (ending !== null) {
									vm.spinner = false;
									vm.response.Items = ending.Items;
									vm.response.Time = ending.Time;
									vm.response.Date = ending.Date;
									vm.response.Cancelled = ending.Cancelled;
									return;
								}
							}
							vm.getStatus();
						}, vm.getStatus);
				}, 100);
			}
		}
		vm.reset = function() {
			vm.response = new Object();
			vm.operationId = null;
			vm.isPreview = false;
		}
		vm.toggle = function (el) {
			for (var k in vm.response) {
				if (typeof (vm.response[k]) === "object")
					vm.response[k].show = false;
			}
			vm.response[el].show = true;
		}
		vm.reset();
	}
})();
