import React from 'react'
import { Text, View, Image, FlatList, TouchableHighlight } from 'react-native'
import { TableData } from '../../models'
import { styles } from './ResultTable.style'
import { UnknownProduct, ShelfGap } from '../../../constants'

interface TableProps {
    data: Array<TableData>;
}

export const ResultTable = (params: TableProps) => {
    let tableData = params.data ?? [];
  
    return (
        <View style={styles.container}>
            <View style={styles.tableHeader}>
                <Text numberOfLines={1} style={[styles.text, { flex: 4 }]}>Object tag</Text>
                <Text numberOfLines={1} style={[styles.text, { flex: 1, textAlign: 'center' }]}>Expected</Text>
                <Text numberOfLines={1} style={[styles.text, { flex: 1, textAlign: 'center' }]}>Count</Text>
            </View>
            <View style={styles.headerLine} />

            <FlatList
                data={tableData}
                renderItem={({ item }) => {

                    const isUnknownProduct = item.Name.toLocaleLowerCase() === UnknownProduct.toLocaleLowerCase();
                    const isShelfGap = item.Name.toLocaleLowerCase() === ShelfGap.toLocaleLowerCase();

                    return <View style={{ margin: 4 }}>
                                <TouchableHighlight onPress={(e) => { console.log('table - selected item: ', item); }}>
                                    <View style={{ flexDirection: 'row'}}>
                                        <View style={{ padding: 10 }}>
                                            { !isUnknownProduct && !isShelfGap &&
                                                <Image style={styles.imageThumbnail} source={{ uri: item.ImageUrl }} />
                                            }
                                            { isUnknownProduct &&
                                                <Image style={styles.imageThumbnail} source={require('../../assets/product.jpg')} />
                                            }
                                            { isShelfGap &&
                                                <Image style={styles.imageThumbnail} source={require('../../assets/gap.jpg')} />
                                            }
                                        </View>
                                        
                                        <Text numberOfLines={1} style={[styles.text, { flex: 3, alignSelf: 'center', padding: 5 }]}>{item.Name}</Text>
                                        <Text style={[styles.text, { flex: 1, textAlign: 'center', alignSelf: 'center' }]}>{item.ExpectedCount}</Text>
                                        <Text style={[styles.text, { flex: 1, textAlign: 'center', alignSelf: 'center' }]}>{item.TotalCount}</Text>
                                    </View>
                                </TouchableHighlight>

                                <View style={styles.rowLine} />
                            </View>
                }}
                //Setting the number of column
                numColumns={1}
                keyExtractor={(item, index) => index.toString()}
            />

        </View>
    )
}
