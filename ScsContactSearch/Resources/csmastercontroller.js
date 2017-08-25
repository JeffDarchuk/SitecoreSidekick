(function () {
	'use strict';

	angular
        .module('app')
        .controller('csmastercontroller', csmastercontroller);

	csmastercontroller.$inject = ['csFactory', '$scope', '$cookies', '$interval'];

	function csmastercontroller(csFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		vm.getResults = function (query) {
			vm.data = false;
			vm.spinner = true;
			csFactory.query(query).then(function (results) {
				vm.data = results.data[0].Value;
				for (var i = 0; i < vm.data.length; i++) {

					vm.data[i].displayName = "";
					vm.data[i].experienceProfileLink = "/sitecore/client/Applications/ExperienceProfile/contact?cid=" + vm.data[i].obj._id;
					try {
						var str = vm.data[i].obj.Personal.FirstName;
						if (str === "" || typeof str === "undefined")
							vm.data[i].displayName += "Unknown ";
						else
							vm.data[i].displayName += str + " ";
					} catch (e) { vm.data[i].displayName += "Unknown " }
					try {
						var str = vm.data[i].obj.Personal.Surname;
						if (str === "" || typeof str === "undefined")
							vm.data[i].displayName += "Unknown ";
						else
							vm.data[i].displayName += str + " ";
					} catch (e) { vm.data[i].displayName += "Unknown " }
					try {
						var str = vm.data[i].obj.Identifiers.Identifier;
						if (str === "" || typeof str === "undefined")
							vm.data[i].displayName += "🢂 Unidentified";
						else
							vm.data[i].displayName += "🢂" + str;
					} catch (e) { vm.data[i].displayName += "🢂 Unidentified" }


				}
				vm.spinner = false;
			});
		}
	}
})();
