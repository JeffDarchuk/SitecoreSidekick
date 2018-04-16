(function () {
	'use strict';

	angular
        .module('app')
        .controller('hgmastercontroller', hgmastercontroller);

	hgmastercontroller.$inject = ['hgFactory', '$scope', '$cookies', '$interval', 'ScsFactory'];

	function hgmastercontroller(hgFactory, $scope, $cookies, $interval, ScsFactory) {
		/* jshint validthis:true */
		var vm = this;
		vm.treeEvents = {
			'click': function (val, property) {
				property.Value = val.Id;
			}
		};
		vm.getTemplates = function () {
			hgFactory.getTemplates().then(function (result) {
				vm.templates = result.data;
				if (vm.templates.length === 1) {
					vm.selectedTemplate = vm.templates[0];
				}
			});
		}
		
		vm.getProperties = function () {
			hgFactory.getProperties(vm.selectedTemplate, vm.selectedTargetKey).then(function (result) {
				vm.properties = result.data;
				vm.propertyMap = new Object();
				for (var i = 0; i < vm.properties.length; i++) {
					vm.propertyMap[vm.properties[i].Id] = vm.properties[i];
					if (vm.properties[i].Value.indexOf("<||>") !== -1) {
						vm.properties[i].Value = vm.properties[i].Value.split("<||>");
					}
					if (vm.properties[i].Value === "true") {
						vm.properties[i].Value = true;
					} else if (vm.properties[i].Processor === "UserInputCheckbox") {
						vm.properties[i].Value = false;
					} else if (vm.properties[i].Processor === "ScTree") {
						if (vm.properties[i].Value !== "") {
							vm.properties[i].Value = vm.properties[i].Value.toLowerCase();
							if (vm.properties[i].Value.startsWith("{")) {
								vm.properties[i].Value = vm.properties[i].Value.substring(1, vm.properties[i].Value.length - 2);
							}
							var prop = vm.properties[i];
							ScsFactory.contentTreeSelectedRelated([vm.properties[i].Value]).then(function(response) {
								prop.relatedIds = response.data;
							});
						}
					}
				}
			});
		}
		vm.removeTemplate = function () {
			if (confirm("You sure you want to delete " + vm.selectedTemplate + "?")) {
				hgFactory.removeTemplate(vm.selectedTemplate).then(function (result) {
					vm.getTemplates();
				});
			}
		}
		vm.submitTargetProp = function (propertyId, value) {
			if (vm.allPropsFalse(vm.targetProps)) {
				vm.editTarget = false;
			}
			hgFactory.submitTargetProp(vm.selectedTargetKey, vm.selectedTemplate, propertyId, value);
		}
		vm.execute = function () {
			for (var i = 0; i < vm.properties.length; i++) {
				delete vm.properties[i].AngularMarkup;
				delete vm.properties[i].Values;
				delete vm.properties[i].Name;
				delete vm.properties[i].Description;
				delete vm.properties[i].list;
				vm.configure = false;
				vm.executing = true;
				if (Array.isArray(vm.properties[i].Value)) {
					vm.properties[i].Value = vm.properties[i].Value.join('<||>');
				}
			}
			for (var o in vm.selectedTarget) {
				vm.properties.push({ "Id": o, "Value": vm.selectedTarget[o] });
			}
			hgFactory.execute(vm.properties, vm.selectedTemplate, vm.selectedTargetKey).then(function(result) {
				vm.executing = false;
				vm.finalize = result.data;
			});
		}
		vm.getTargets = function () {
			hgFactory.getTargets(vm.selectedTemplate).then(function (result) {
				vm.targets = result.data;
				if (vm.targets.length === 1) {
					vm.selectedTarget = vm.targets[0];
				}
			});
		}
		vm.resetTarget = function(key, target) {
			vm.selectedTarget = target;
			vm.selectedTargetKey = key;
			vm.targetProps = new Object();
			for (var targetKey in vm.selectedTarget) {
				if (targetKey.indexOf("-o") === -1 && vm.selectedTarget[targetKey] === "???") {
					vm.targetProps[targetKey] = true;
				} else {
					vm.targetProps[targetKey] = false;
				}
			}
			vm.getProperties();
		}
		vm.downloadTemplate = function () {
			var downloadFrame = document.getElementById("downloadframe");
			downloadFrame.src = "/scs/hg/hgdownloadtemplate.scsvc?template=" + vm.selectedTemplate;
		}
		vm.uploadTemplate = function () {
			var data = document.getElementById("templateUpload");
			var file = data.files[0];
			var xhr = new XMLHttpRequest();
			xhr.open("POST", "/scs/hg/hguploadtemplate.scsvc", true);
			xhr.setRequestHeader("X_FILENAME", file.name);
			xhr.onreadystatechange = function () {
				if (xhr.readyState === XMLHttpRequest.DONE && xhr.status === 200) {
					vm.getTemplates();
				}
			};
			xhr.send(file);

		}
		vm.allPropsFalse = function (o) {
			if (!vm.selectedTarget)
				return false;
			var any = false;
			for (var key in o) {
				any = true;
				if (o[key])
					return false;
			}
			return any;
		}
		vm.getProjects = function(property) {
			hgFactory.getProjects(vm.selectedTarget["_SOLUTIONPATH_"]).then(function(result) {
				property.projects = result.data;
			});
		}
		vm.getControllers = function (property) {
			if (property.Value[0]) {
				hgFactory.getControllers(property.Value[0]).then(function(result) {
					property.controllers = result.data;
				});
			}
		}
		vm.initArray = function (property) {
			if (typeof (property.Value) !== 'object') {
				property.Value = ['', ''];
			}
		}
		vm.reset = function () {
			vm.configure = false;
			vm.executing = false;
			vm.setup = true;
			vm.editTarget = false;
			vm.selectedTemplate = "";
			vm.selectedTarget = "";
			vm.selectedTargetKey = "";
			vm.projects = new Array();
			vm.controllers = new Array();
			vm.templates = new Array();
			vm.targets = new Array();
			vm.finalize = false;
			vm.targetProps = new Object();
			vm.getTemplates();
			vm.properties = false;
		}
		vm.reset();
	}
})();
