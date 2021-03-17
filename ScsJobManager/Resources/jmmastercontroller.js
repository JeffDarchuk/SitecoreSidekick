(function () {
	'use strict';

	angular
        .module('app')
        .controller('jmmastercontroller', jmmastercontroller);

	jmmastercontroller.$inject = ['jmFactory', '$scope', '$cookies', '$interval'];

	function jmmastercontroller(jmFactory, $scope, $cookies, $interval) {
		/* jshint validthis:true */
		var vm = this;
		vm.jobs = new Array();
		jmFactory.getJobs().then(function (response) {
			vm.jobs = response.data;
		});
		vm.cancelJob = function(name) {
			jmFactory.cancelJob(name).then(function(response) {
				alert('ok');
			});
		}
	}
})();
