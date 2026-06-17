// IndexedDB LocalStore module - loaded on demand via import

/**
 * Opens the IndexedDB database, creating it if necessary.
 * @param {string} dbName - Database name
 * @param {string} storeName - Object store name
 * @param {number} version - Database version
 * @returns {Promise<IDBDatabase>} The opened database
 */
function openDatabase(dbName, storeName, version) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(dbName, version);

        request.onerror = () => {
            reject(new Error(`Failed to open IndexedDB: ${request.error?.message || 'Unknown error'}`));
        };

        request.onsuccess = () => {
            resolve(request.result);
        };

        request.onupgradeneeded = (event) => {
            const db = event.target.result;
            if (!db.objectStoreNames.contains(storeName)) {
                const store = db.createObjectStore(storeName, { keyPath: ['group', 'id'] });
                store.createIndex('group', 'group', { unique: false });
            }
        };
    });
}

/**
 * Gets a value from the store.
 * @param {string} dbName - Database name
 * @param {string} storeName - Object store name
 * @param {number} version - Database version
 * @param {string} group - Group key
 * @param {string} id - Item ID
 * @returns {Promise<string|null>} The stored data or null
 */
export async function get(dbName, storeName, version, group, id) {
    const db = await openDatabase(dbName, storeName, version);
    try {
        return await new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const request = store.get([group, id]);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result?.data ?? null);
        });
    } finally {
        db.close();
    }
}

/**
 * Gets all IDs for a group.
 * @param {string} dbName - Database name
 * @param {string} storeName - Object store name
 * @param {number} version - Database version
 * @param {string} group - Group key
 * @returns {Promise<string[]>} Array of IDs
 */
export async function getIds(dbName, storeName, version, group) {
    const db = await openDatabase(dbName, storeName, version);
    try {
        return await new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const index = store.index('group');
            const request = index.getAll(IDBKeyRange.only(group));

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const ids = request.result.map(item => item.id);
                resolve(ids);
            };
        });
    } finally {
        db.close();
    }
}

/**
 * Checks if an entry exists.
 * @param {string} dbName - Database name
 * @param {string} storeName - Object store name
 * @param {number} version - Database version
 * @param {string} group - Group key
 * @param {string} id - Item ID
 * @returns {Promise<boolean>} True if exists
 */
export async function contains(dbName, storeName, version, group, id) {
    const db = await openDatabase(dbName, storeName, version);
    try {
        return await new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readonly');
            const store = transaction.objectStore(storeName);
            const request = store.count([group, id]);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result > 0);
        });
    } finally {
        db.close();
    }
}

/**
 * Commits a batch of operations atomically from a JSON string.
 * @param {string} dbName - Database name
 * @param {string} storeName - Object store name
 * @param {number} version - Database version
 * @param {string} operationsJson - JSON string of operations to commit
 */
export async function commitOperationsJson(dbName, storeName, version, operationsJson) {
    const operations = JSON.parse(operationsJson);
    
    if (!operations || operations.length === 0) {
        return;
    }

    const db = await openDatabase(dbName, storeName, version);
    try {
        await new Promise((resolve, reject) => {
            const transaction = db.transaction(storeName, 'readwrite');
            const store = transaction.objectStore(storeName);

            transaction.onerror = () => reject(transaction.error);
            transaction.oncomplete = () => resolve();

            for (const op of operations) {
                if (op.type === 'set') {
                    store.put({
                        group: op.group,
                        id: op.id,
                        data: op.data
                    });
                } else if (op.type === 'delete') {
                    store.delete([op.group, op.id]);
                }
            }
        });
    } finally {
        db.close();
    }
}
