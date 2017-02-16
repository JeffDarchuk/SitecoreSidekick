(function () {
	'use strict';

	angular
        .module('app')
        .factory('CMfactory', CMfactory);

	CMfactory.$inject = ['$http'];

	function CMfactory($http) {
		var service = {

			contentTree: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cmcontenttree.scsvc", data);
			},
			contentTreeGetItem: function (id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/cmcontenttreegetitem.scsvc", data);
			},
			contentTreePullItem: function (ids, database, server, children, overwrite, pullParent, mirror, preview, eventDisabler, bulkUpdate) {
				var data = { "ids": ids, "database": database, "server": server, "children": children, "overwrite": overwrite, "pullParent": pullParent, "mirror": mirror, "preview": preview, "eventDisabler": eventDisabler, "bulkUpdate": bulkUpdate };
				return $http.post("/scs/cmcontenttreepullitem.scsvc", data);
			},
			contentTreeServerList: function () {
				return $http.get("/scs/cmserverlist.scsvc");
			},
			operationStatus: function (operationId, lineNumber) {
				var data = { "operationId": operationId, "lineNumber": lineNumber };
				return $http.post("/scs/cmopeartionstatus.scsvc", data);
			},
			operations: function() {
				return $http.get("/scs/cmoperationlist.scsvc");
			},
			stopOperation: function(operationId) {
				var data = { "operationId": operationId };
				return $http.post("/scs/cmstopoperation.scsvc", data);
			},
			runPreviewAsPull: function(operationId) {
				var data = { "operationId": operationId };
				return $http.post("/scs/cmapprovepreview.scsvc", data);
			},
			queuedItems: function (operationId) {
				var data = { "operationId": operationId };
				return $http.post("/scs/cmqueuelength.scsvc", data);
			}
		};
		
		return service;

		function getData() { }
	}
})();