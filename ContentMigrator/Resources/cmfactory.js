(function () {
	'use strict';

	angular
        .module('app')
        .factory('CMfactory', CMfactory);

	CMfactory.$inject = ['$http'];

	function CMfactory($http) {
		var service = {

			contentTree: function (id, database, server, standardValues) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cm/cmcontenttree.scsvc", data);
			},
			contentTreeGetItem: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cm/cmcontenttreegetitem.scsvc", data);
			},
			contentTreePullItem: function (ids, database, server, children, overwrite, pullParent, mirror, preview, eventDisabler, bulkUpdate) {
				var data = { "ids": ids, "database": database, "server": server, "children": children, "overwrite": overwrite, "pullParent": pullParent, "removeLocalNotInRemote": mirror, "preview": preview, "eventDisabler": eventDisabler, "bulkUpdate": bulkUpdate };
				return $http.post("/scs/cm/cmstartoperation.scsvc", data);
			},
			contentTreeServerList: function () {
				return $http.get("/scs/cm/cmserverlist.scsvc");
			},
			operationStatus: function (operationId, lineNumber) {
				var data = { "operationId": operationId, "lineNumber": lineNumber };
				return $http.post("/scs/cm/cmopeartionstatus.scsvc", data);
			},
			operationLog: function(operationId, lineNumber) {
				var data = { "operationId": operationId, "lineNumber": lineNumber };
				return $http.post("/scs/cm/cmopeartionlog.scsvc", data);
			},
			operations: function() {
				return $http.get("/scs/cm/cmoperationlist.scsvc");
			},
			stopOperation: function(operationId) {
				return $http.post("/scs/cm/cmstopoperation.scsvc", "'" + operationId + "'");
			},
			runPreviewAsPull: function(operationId) {
				return $http.post("/scs/cm/cmapprovepreview.scsvc", "'" + operationId + "'");
			},
			queuedItems: function (operationId) {
				return $http.post("/scs/cm/cmqueuelength.scsvc", "'" + operationId + "'");
			},
			getDiff: function (id, server) {
				var data = { "id": id, "server": server };
				return $http.post("/scs/cm/cmbuilddiff.scsvc", data);
			}

		};
		
		return service;

		function getData() { }
	}
})();