import React, { Component } from 'react';
import { Text, View, Image, FlatList, TouchableHighlight, StyleSheet } from 'react-native';


const ResultTable = (params) => {
    const { container, tableHeader, text, headerLine, rowLine, imageThumbnail } = styles;
    let tableData = params.data ?? [];
  
    return (
        <View style={container}>
            <View style={tableHeader}>
                <Text numberOfLines={1} style={[text, { flex: 4 }]}>Object tag</Text>
                <Text numberOfLines={1} style={[text, { flex: 1, textAlign: 'center' }]}>Expected</Text>
                <Text numberOfLines={1} style={[text, { flex: 1, textAlign: 'center' }]}>Count</Text>
            </View>
            <View style={headerLine} />

            <FlatList
                data={tableData}
                renderItem={({ item }) => (
                    <View style={{ margin: 4 }}>
                        
                        <TouchableHighlight onPress={(e) => { console.log('table - selected item: ', item); }}>
                            <View style={{ flexDirection: 'row'}}>
                                <View style={{ padding: 10 }}>
                                    { item.Name.toLocaleLowerCase() != 'product' && item.Name.toLocaleLowerCase() != 'gap' &&
                                        <Image style={imageThumbnail} source={{ uri: item.ImageUrl }} />
                                    }
                                    { item.Name.toLocaleLowerCase() == 'product' &&
                                        <Image style={imageThumbnail} source={require('../../assets/product.jpg')} />
                                    }
                                    { item.Name.toLocaleLowerCase() == 'gap' &&
                                        <Image style={imageThumbnail} source={require('../../assets/gap.jpg')} />
                                    }
                                </View>
                                
                                <Text numberOfLines={1} style={[text, { flex: 3, alignSelf: 'center', padding: 5 }]}>{item.Name}</Text>
                                <Text style={[text, { flex: 1, textAlign: 'center', alignSelf: 'center' }]}>{item.ExpectedCount}</Text>
                                <Text style={[text, { flex: 1, textAlign: 'center', alignSelf: 'center' }]}>{item.TotalCount}</Text>
                            </View>
                        </TouchableHighlight>

                        <View style={rowLine} />
                    </View>
                )}
                //Setting the number of column
                numColumns={1}
                keyExtractor={(item, index) => index.toString()}
            />

        </View>
    )
}

const styles = StyleSheet.create({
    container: { flex: 1 },
    tableHeader: {
        flexDirection: 'row', 
        marginBottom: 10
    },
    text: {
        color: 'white', 
        opacity: 0.6
    },
    headerLine: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.6
    },
    rowLine: {
        height: 1, 
        backgroundColor: 'white', 
        opacity: 0.2
    },
    imageThumbnail: {
        justifyContent: 'center',
        alignItems: 'center',
        height: 40,
        width: 40
    }
})

export { ResultTable };