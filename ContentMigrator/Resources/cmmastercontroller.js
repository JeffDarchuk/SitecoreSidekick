(function () {
	'use strict';

	angular
		.module('app')
		.controller('cmmastercontroller', cmmastercontroller);

	cmmastercontroller.$inject = ['CMfactory', '$scope', '$timeout', '$window', 'ScsFactory'];

	function cmmastercontroller(CMfactory, $scope, $timeout, $window, ScsFactory) {
		/* jshint validthis:true */
		var vm = this;
		vm.children = true;
		vm.overwrite = true;
		vm.pullParent = true;
		vm.mirror = false;
		vm.eventDisabler = true;
		vm.bulkUpdate = true;
		vm.server = "";
		vm.spinner = false;
		vm.isPreview = false;
		vm.displayLog = false;
		vm.events = {
			'selectedIds': [],
			'selected': [],
			'difflang': 'none',
			'diffreset': true,
			'standardValues': false,
			'showAll': true,
			'validateDiffRow': function (field, diff) {
				if (!this.standardValues && field.substring(0, 2) === "__") return false;
				if (!this.standardValues && diff === "~") return false;
				return true;
			},
			'click': function (val) {
				delete vm.events.relatedIds;

				if (val.MissingRemote)
					return;
				if (!vm.events.control) {
					vm.events.selected = [];
					vm.events.selectedIds = [];
				}
				var index = vm.events.selectedIds.indexOf(val.Id);
				if (index === -1) {
					vm.events.selected.push(val);
					vm.events.selectedIds.push(val.Id);
				} else {
					vm.events.selected.splice(index, 1);
					vm.events.selectedIds.splice(index, 1);
				}
				ScsFactory.contentTreeSelectedRelated(vm.events.selectedIds, vm.server).then(function (response) {
					vm.events.relatedIds = response.data;
				});
			}
		};
		angular.element($window)
			.bind("keydown",
			function ($event) {
				vm.events.control = $event.ctrlKey;
			});
		angular.element($window)
			.bind("keyup",
			function ($event) {
				vm.events.control = false;
			});
		angular.element($window)
			.bind("mousedown",
			function ($event) {
				var target = $event.target;
				while (target && target.tagName !== "body") {
					if (target.className && target.className.indexOf("cmdifftableroot") > -1) {
						return;
					}
					target = target.parentNode;
				}
				vm.resultDiff = false;
				vm.events.difflang = 'clean';
				vm.events.diff = false;
			});
		vm.pull = function (preview) {
			if (preview || confirm("Are you sure you would like to pull content from the items " + vm.listSources())) {
				vm.spinner = true;
				if (!vm.serverModified && vm.events.selected && vm.events.selectedIds.length > 0)
					CMfactory.contentTreePullItem(vm.events.selectedIds, vm.events.selected[0].DatabaseName, vm.events.server, vm.children, vm.overwrite, vm.pullParent, vm.mirror, preview, vm.eventDisabler, vm.bulkUpdate).then(function (response) {
						vm.streamResults(response.data, vm.events.server, vm.listIds(), vm.listSources(), preview);
					}, function (response) {
						vm.error = response.data;
					});
				else {
					vm.spinner = false;
					vm.response = { "Error": "Unable to pull from selected remote sitecore node." };
				}
			}
		}
		vm.initializeDiff = function (events, id) {
			vm.resultDiff = events;
			vm.resultId = id;
			for (var el in events) {
				vm.events.difflang = el;
				break;
			}
		}
		vm.listSources = function () {
			var ret = [];
			for (var i = 0; i < vm.events.selected.length; i++) {
				ret.push(vm.events.selected[i].DisplayName);
			}
			return ret.join(", ");
		}
		vm.listIds = function () {
			var ret = [];
			for (var i = 0; i < vm.events.selected.length; i++) {
				ret.push(vm.events.selected[i].Id);
			}
			return ret.join(", ");
		}
		vm.streamResults = function (id, server, itemId, name, preview) {
			vm.operationId = id;
			vm.spinner = true;
			vm.getStatus();
			vm.streaming = new Object();
			vm.streaming.server = server;
			if (typeof (itemId) === "string")
				vm.streaming.id = itemId;
			else {
				var ret = [];
				for (var i = 0; i < itemId.length; i++) {
					ret.push(itemId[i].Id);
				}
				vm.streaming.id = ret.join(", ");
			}
			if (typeof (name) === "string")
				vm.streaming.name = name;
			else {
				var ret = [];
				for (var i = 0; i < itemId.length; i++) {
					ret.push(itemId[i].DisplayName);
				}
				vm.streaming.name = ret.join(", ");
			}
			vm.response = new Object();
			vm.response.lineNumber = 0;
			vm.response.log = [];
			vm.response.viewingNone = true;
			vm.isPreview = preview;
		}
		vm.stopOperation = function () {
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
		vm.GetOperationsInProgress = function () {
			CMfactory.operations().then(function (response) {
				vm.completedOperations = new Array();
				vm.runningOperations = new Array();
				vm.previewOperations = new Array();
				vm.cancelledOperations = new Array();
				for (var i = 0; i < response.data.length; i++) {
					var tmp = new Object();
					tmp["rootNodes"] = response.data[i].RootNodes;
					tmp["operationId"] = response.data[i].OperationId;
					tmp["started"] = new Date(Date.parse(response.data[i].StartedTime)).toString();
					tmp["server"] = response.data[i].Server;
					if (response.data[i].IsPreview) {
						vm.previewOperations.push(tmp);
					} else if (response.data[i].Cancelled) {
						vm.cancelledOperations.push(tmp);
					} else if (response.data[i].Completed) {
						vm.completedOperations.push(tmp);
					} else {
						vm.runningOperations.push(tmp);
					}
				}
				vm.operationList = response.data;
				if (scsActiveModule === "Content Migrator")
					setTimeout(function () { vm.GetOperationsInProgress() }, 1000);
			});
		}
		vm.runPreview = function () {
			if (confirm("Are you sure you would like to execute this preview operation?")) {
				CMfactory.runPreviewAsPull(vm.operationId);
				vm.streamResults(vm.operationId, vm.streaming.server, vm.streaming.id, vm.streaming.name, false);
			}
		}
		vm.GetOperationsInProgress();
		vm.getStatus = function () {
			if (vm.operationId) {
				setTimeout(function () {
					if (vm.response.lineNumber > -1)
						CMfactory.queuedItems(vm.operationId).then(function (response) {
							vm.queuedItems = response.data;
						});
					if (!vm.displayLog) {
						CMfactory.operationStatus(vm.operationId, vm.response.lineNumber).then(function(response) {
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
												vm.response[response.data[i].Operation].displayName = response.data[i].Operation.replace(/_/g, " ");
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
							},
							vm.getStatus);
					} else {
						CMfactory.operationLog(vm.operationId, vm.response.log.length).then(function(response) {
							for (var i = 0; i < response.data.length; i++) {
								var entry = response.data[i];
								if (entry[0] !== "{") {
									var parts = entry.split("]");
									parts[0] += ']';
									vm.response.log.push(parts);
									continue;
								}
								var ending = JSON.parse(entry);
								vm.spinner = false;
								vm.response.Items = ending.Items;
								vm.response.Time = ending.Time;
								vm.response.Date = ending.Date;
								vm.response.Cancelled = ending.Cancelled;
								return;
							}
							vm.getStatus();
						});
					}
				}, 500);
			}
		}
		vm.reset = function () {
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
		vm.isEmptyObject = function (obj) {
			for (var prop in obj) {
				if (Object.prototype.hasOwnProperty.call(obj, prop)) {
					return false;
				}
			}
			return true;
		}
		vm.reset();
	}
})();
