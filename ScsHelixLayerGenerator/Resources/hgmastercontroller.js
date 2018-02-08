(function () {
	'use strict';

	angular
        .module('app')
        .controller('hgmastercontroller', hgmastercontroller);

	hgmastercontroller.$inject = ['hgFactory', '$scope', '$cookies', '$interval'];

	function hgmastercontroller(hgFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		vm.getTemplates = function () {
			hgFactory.getTemplates().then(function (result) {
				vm.templates = result.data;
				if (vm.templates.length == 1) {
					vm.selectedTemplate = vm.templates[0];
				}
			});
		}
		
		vm.getProperties = function () {
			hgFactory.getProperties(vm.selectedTemplate).then(function (result) {
				vm.properties = result.data;
				vm.propertyMap = new Object();
				for (var i = 0; i < vm.properties.length; i++) {
					vm.propertyMap[vm.properties[i].Id] = vm.properties[i];
					if (vm.properties[i].Value === "true") {
						vm.properties[i].Value = true;
					} else if (vm.properties[i].Process === "UserInputCheckbox") {
						vm.properties[i].Value = false;
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
					vm.properties[i].Value = vm.properties[i].Value.join();
				}
			}
			for (var o in vm.selectedTarget) {
				vm.properties.push({ "Id": o, "Value": vm.selectedTarget[o] })
			}
			hgFactory.execute(vm.properties, vm.selectedTemplate, vm.selectedTargetKey).then(function (result) {
				vm.executing = false;
				vm.finalize = true;
			})
		}
		vm.getTargets = function () {
			hgFactory.getTargets(vm.selectedTemplate).then(function (result) {
				vm.targets = result.data;
				if (vm.targets.length == 1) {
					vm.selectedTarget = vm.targets[0];
				}
			});
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
		vm.reset = function () {
			vm.configure = false;
			vm.executing = false;
			vm.setup = true;
			vm.selectedTemplate = "";
			vm.selectedTarget = ""
			vm.selectedTargetKey = "";
			vm.templates = new Array();
			vm.targets = new Array();
			vm.getTemplates();
		}
		vm.reset();
	}
})();
