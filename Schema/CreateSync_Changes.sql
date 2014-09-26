CREATE TABLE `Sync_Changes` (
`id` int(10) unsigned NOT NULL auto_increment,
`schema` varchar(255) NOT NULL,
`table` varchar(255) NOT NULL,
`operation` varchar(255) NOT NULL,
`pk1` int(10) unsigned NOT NULL,
`pk2` int(10) unsigned NULL,
`status` int(2) unsigned NOT NULL,
`transactionId` int(10) unsigned NOT NULL,
`datetime` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
PRIMARY KEY (`id`)
);
